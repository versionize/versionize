using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using McMaster.Extensions.CommandLineUtils;
using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.Config.Validation;

namespace Versionize.Commands;

[Command(Name = "init", Description = "Initialize a .versionize configuration for monorepos")]
internal sealed class InitCommand(CliConfig cliConfig)
{
    private readonly CliConfig _cliConfig = cliConfig;

    [Option("--force", Description = "Overwrite existing .versionize file if it exists")]
    public bool Force { get; }

    [Option("--force-config", Description = "Create .versionize even for single-project repositories")]
    public bool ForceConfig { get; }

    [Option("--version-element <VERSION_ELEMENT>", Description = "Version element to add or update in project files (default: Version)")]
    public string? VersionElement { get; }

    [Option("--tag-template <TAG_TEMPLATE>", Description = "Tag template to use for each project (default: {name}-v{version})")]
    public string? TagTemplate { get; }

    [Option("--initial-version <VERSION>", Description = "Initial version to write when missing (default: 0.0.0)")]
    [SemanticVersion]
    public string? InitialVersion { get; }

    [Option("--skip-project-update", Description = "Do not modify project files; only generate .versionize")]
    public bool SkipProjectUpdate { get; }

    public int OnExecute()
    {
        var cwd = _cliConfig.WorkingDirectory.Value() ?? Directory.GetCurrentDirectory();
        var configDir = _cliConfig.ConfigurationDirectory.Value() ?? cwd;
        var configPath = Path.Combine(configDir, ".versionize");
        var versionElement = string.IsNullOrWhiteSpace(VersionElement) ? "Version" : VersionElement;
        var tagTemplate = string.IsNullOrWhiteSpace(TagTemplate) ? "{name}-v{version}" : TagTemplate;
        var initialVersion = string.IsNullOrWhiteSpace(InitialVersion) ? "0.0.0" : InitialVersion;
        var dryRun = _cliConfig.DryRun.HasValue() && _cliConfig.DryRun.ParsedValue;

        if (_cliConfig.Silent.HasValue() && _cliConfig.Silent.ParsedValue)
        {
            CommandLineUI.Verbosity = LogLevel.Error;
        }

        if (!SemanticVersion.TryParse(initialVersion, out _))
        {
            return CommandLineUI.Exit(ErrorMessages.InvalidVersionFormat(initialVersion), 1);
        }

        ValidateVersionElement(versionElement);

        if (!tagTemplate.Contains("{version}", StringComparison.OrdinalIgnoreCase))
        {
            return CommandLineUI.Exit(ErrorMessages.InvalidTagTemplate(tagTemplate), 1);
        }

        if (!BumpFileProvider.IsDotnetProject(cwd))
        {
            return CommandLineUI.Exit(ErrorMessages.NoProjectFilesFound(cwd), 1);
        }

        var allProjectFiles = BumpFileProvider.DiscoverProjectFiles(cwd).ToList();
        var testProjectFiles = allProjectFiles.Where(IsTestProject).ToList();
        var projectFiles = allProjectFiles.Where(projectFile => !IsTestProject(projectFile)).ToList();

        if (testProjectFiles.Count > 0)
        {
            CommandLineUI.Information("Skipping test projects:");
            foreach (var testProject in testProjectFiles)
            {
                var relativePath = Path.GetRelativePath(cwd, testProject)
                    .Replace(Path.DirectorySeparatorChar, '/');
                CommandLineUI.Information($"  * {relativePath}");
            }
        }
        if (projectFiles.Count == 0)
        {
            return CommandLineUI.Exit(ErrorMessages.NoProjectFilesFound(cwd), 1);
        }

        var initialProjects = projectFiles
            .Select(projectFile => DotnetBumpFileProject.CreateInitial(projectFile, versionElement))
            .ToList();

        if (projectFiles.Count == 1 && !ForceConfig)
        {
            var project = initialProjects[0];

            if (!SkipProjectUpdate)
            {
                var updated = DotnetBumpFileProject.EnsureVersionElement(project.ProjectFile, versionElement, initialVersion, dryRun);
                if (updated)
                {
                    CommandLineUI.Step($"updated 1 project file with <{versionElement}>{initialVersion}</{versionElement}>");
                }
                else if (!project.HasVersion && project.VersionError != null)
                {
                    CommandLineUI.Information(project.VersionError);
                }
            }

            var versionText = project.HasVersion ? project.Version.ToNormalizedString() : initialVersion;
            CommandLineUI.Step("single project detected; no .versionize file created");
            CommandLineUI.Information("versionize can be used without any further configurations");
            CommandLineUI.Information("use --force-config to write a .versionize file anyway");
            CommandLineUI.Information($"project version: {versionText}");
            return 0;
        }

        if (File.Exists(configPath) && !Force)
        {
            return CommandLineUI.Exit(ErrorMessages.VersionizeConfigAlreadyExists(configPath), 1);
        }

        var projects = BuildProjects(projectFiles, cwd, tagTemplate, versionElement);
        var config = new FileConfig
        {
            Projects = [.. projects],
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        });

        if (dryRun)
        {
            CommandLineUI.DryRun(json);
        }
        else
        {
            Directory.CreateDirectory(configDir);
            File.WriteAllText(configPath, json);
            CommandLineUI.Step($"wrote {configPath}");
        }

        if (!SkipProjectUpdate)
        {
            var updatedCount = 0;

            foreach (var project in initialProjects)
            {
                var updated = DotnetBumpFileProject.EnsureVersionElement(project.ProjectFile, versionElement, initialVersion, dryRun);
                if (updated)
                {
                    updatedCount++;
                }
                else if (!project.HasVersion && project.VersionError != null)
                {
                    CommandLineUI.Information(project.VersionError);
                }
            }

            if (updatedCount > 0)
            {
                CommandLineUI.Step($"updated {updatedCount} project file(s) with <{versionElement}>{initialVersion}</{versionElement}>");
            }
        }

        return 0;
    }

    private static IEnumerable<ProjectOptions> BuildProjects(
        IEnumerable<string> projectFiles,
        string workingDirectory,
        string tagTemplate,
        string versionElement)
    {
        var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var projectFile in projectFiles.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var fileName = Path.GetFileNameWithoutExtension(projectFile);
            var name = fileName.ToLowerInvariant();
            name = EnsureUniqueName(name, nameCounts);

            var projectDir = Path.GetDirectoryName(projectFile) ?? workingDirectory;
            var relativePath = Path.GetRelativePath(workingDirectory, projectDir);
            if (relativePath == ".")
            {
                relativePath = string.Empty;
            }

            var normalizedPath = relativePath.Replace(Path.DirectorySeparatorChar, '/');
            var header = string.IsNullOrWhiteSpace(fileName)
                ? "Changelog"
                : $"{ToTitleCase(fileName)} Changelog";

            yield return new ProjectOptions
            {
                Name = name,
                Path = normalizedPath,
                TagTemplate = tagTemplate,
                VersionElement = versionElement == "Version" ? null : versionElement,
                Changelog = new ChangelogOptions
                {
                    Header = header,
                    Sections =
                    [
                        new ChangelogSection { Type = "feat", Section = "Features", Hidden = false },
                        new ChangelogSection { Type = "fix", Section = "Bug Fixes", Hidden = false },
                        new ChangelogSection { Type = "perf", Section = "Performance", Hidden = false },
                    ],
                },
            };
        }
    }

    private static string EnsureUniqueName(string name, IDictionary<string, int> nameCounts)
    {
        if (!nameCounts.TryGetValue(name, out var count))
        {
            nameCounts[name] = 1;
            return name;
        }

        count++;
        nameCounts[name] = count;
        return $"{name}-{count}";
    }

    private static string ToTitleCase(string value)
    {
        var culture = CultureInfo.InvariantCulture;
        return culture.TextInfo.ToTitleCase(value);
    }

    private static void ValidateVersionElement(string versionElement)
    {
        foreach (var ch in versionElement)
        {
            if (!(char.IsLetterOrDigit(ch) || ch == '_'))
            {
                throw new VersionizeException(ErrorMessages.InvalidVersionElement(versionElement), 1);
            }
        }
    }

    private static bool IsTestProject(string projectFile)
    {
        var fileName = Path.GetFileNameWithoutExtension(projectFile);
        if (fileName.Contains(".Test", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains(".Tests", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith("Test", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith("Tests", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var directory = Path.GetDirectoryName(projectFile) ?? string.Empty;
        var segments = directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment =>
            segment.Equals("test", StringComparison.OrdinalIgnoreCase) ||
            segment.Equals("tests", StringComparison.OrdinalIgnoreCase));
    }

}
