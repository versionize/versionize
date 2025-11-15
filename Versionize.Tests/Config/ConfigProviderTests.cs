using McMaster.Extensions.CommandLineUtils;
using Shouldly;
using Versionize.CommandLine;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Config;

public class ConfigProviderTests : IDisposable
{
    private readonly TestSetup _testSetup;
    private readonly TestPlatformAbstractions _testPlatformAbstractions;

    public ConfigProviderTests()
    {
        _testSetup = TestSetup.Create();

        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
    }

    [Fact]
    public void ShouldExitIfChangelogPathsPointingToSameLocation()
    {
        var projects = new[]
        {
            new ProjectOptions
            {
                Name = "Project1",
                Path = "project1",
                Changelog = ChangelogOptions.Default with
                {
                    Header = "Project1 header",
                    Path = "../docs"
                }
            },
            new ProjectOptions
            {
                Name = "Project2",
                Path = "project2",
                Changelog = ChangelogOptions.Default with
                {
                    Header = "Project2 header",
                    Path = "../docs"
                }
            }
        };

        var fileConfig = new FileConfig
        {
            SkipDirty = true,
            Projects = projects
        };

        var cliConfig = CliConfig.Create(new CommandLineApplication());

        Should.Throw<VersionizeException>(() => ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig))
            .Message.ShouldBe(ErrorMessages.DuplicateChangelogPaths());
    }

    [Theory]
    [InlineData(new[] { "--tag-only" }, false, BumpFileType.None)]
    [InlineData(new[] { "--tag-only" }, true, BumpFileType.None)]
    [InlineData(new[] { "--tag-only" }, null, BumpFileType.None)]
    [InlineData(new string[] { }, true, BumpFileType.None)]
    [InlineData(new string[] { }, false, BumpFileType.Unity)]
    [InlineData(new string[] { }, null, BumpFileType.Unity)]
    public void ReturnsUnityBumpFileTypeWhenTagOnlyIsFalse(string[] cliInput, bool? fileTagOnly, BumpFileType expectedBumpFileType)
    {
        TempProject.CreateUnityProject(_testSetup.WorkingDirectory);
        var fileConfig = new FileConfig { TagOnly = fileTagOnly };
        var cliApp = new CommandLineApplication();
        var cliConfig = CliConfig.Create(cliApp);
        cliApp.Parse(cliInput);
        VersionizeOptions options = ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig);
        options.BumpFileType.ShouldBe(expectedBumpFileType);
    }

    [Theory]
    [InlineData(new[] { "--tag-only" }, false, BumpFileType.None)]
    [InlineData(new[] { "--tag-only" }, true, BumpFileType.None)]
    [InlineData(new[] { "--tag-only" }, null, BumpFileType.None)]
    [InlineData(new string[] { }, true, BumpFileType.None)]
    [InlineData(new string[] { }, false, BumpFileType.Dotnet)]
    [InlineData(new string[] { }, null, BumpFileType.Dotnet)]
    public void ReturnsDotnetBumpFileTypeWhenTagOnlyIsFalse(string[] cliInput, bool? fileTagOnly, BumpFileType expectedBumpFileType)
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var fileConfig = new FileConfig { TagOnly = fileTagOnly };
        var cliApp = new CommandLineApplication();
        var cliConfig = CliConfig.Create(cliApp);
        cliApp.Parse(cliInput);
        VersionizeOptions options = ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig);
        options.BumpFileType.ShouldBe(expectedBumpFileType);
    }

    [Theory]
    [InlineData("--proj-name project1", BumpFileType.Dotnet)]
    [InlineData("--proj-name project2", BumpFileType.Unity)]
    public void ReturnsBumpFileTypeWhenMonoRepo(string cliInput, BumpFileType expectedBumpFileType)
    {
        var projects = new[]
        {
            new ProjectOptions
            {
                Name = "Project1",
                Path = "project1",
                Changelog = ChangelogOptions.Default with
                {
                    Header = "Project1 header",
                }
            },
            new ProjectOptions
            {
                Name = "Project2",
                Path = "project2",
                Changelog = ChangelogOptions.Default with
                {
                    Header = "Project2 header",
                }
            }
        };

        var dotnetProjectPath = Path.Combine(_testSetup.WorkingDirectory, "project1");
        var unityProjectPath = Path.Combine(_testSetup.WorkingDirectory, "project2");
        TempProject.CreateCsharpProject(dotnetProjectPath);
        TempProject.CreateUnityProject(unityProjectPath);
        var fileConfig = new FileConfig { Projects = projects };
        var cliApp = new CommandLineApplication();
        var cliConfig = CliConfig.Create(cliApp);
        cliApp.Parse(cliInput);
        VersionizeOptions options = ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig);
        options.BumpFileType.ShouldBe(expectedBumpFileType);
    }

    [Theory]
    [InlineData("--skip-tag=true", true, true)]
    [InlineData("--skip-tag=false", true, false)]
    [InlineData("--skip-tag", true, true)]
    [InlineData("--skip-tag=true", false, true)]
    [InlineData("--skip-tag=false", false, false)]
    [InlineData("", true, true)]
    [InlineData("", false, false)]
    public void SkipTagCliOptionTakesPriorityOverFileConfig(string cliInput, bool fileConfigValue, bool expectedValue)
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var fileConfig = new FileConfig { SkipTag = fileConfigValue };
        var cliApp = new CommandLineApplication();
        var cliConfig = CliConfig.Create(cliApp);
        cliApp.Parse(string.IsNullOrEmpty(cliInput) ? [] : [cliInput]);
        VersionizeOptions options = ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig);
        options.SkipTag.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("--skip-commit=true", true, true)]
    [InlineData("--skip-commit=false", true, false)]
    [InlineData("--skip-commit", true, true)]
    [InlineData("--skip-commit=true", false, true)]
    [InlineData("--skip-commit=false", false, false)]
    [InlineData("", true, true)]
    [InlineData("", false, false)]
    public void SkipCommitCliOptionTakesPriorityOverFileConfig(string cliInput, bool fileConfigValue, bool expectedValue)
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var fileConfig = new FileConfig { SkipCommit = fileConfigValue };
        var cliApp = new CommandLineApplication();
        var cliConfig = CliConfig.Create(cliApp);
        cliApp.Parse(string.IsNullOrEmpty(cliInput) ? [] : [cliInput]);
        VersionizeOptions options = ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig);
        options.SkipCommit.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("--dry-run=true", true, true)]
    [InlineData("--dry-run=false", true, false)]
    [InlineData("--dry-run", true, true)]
    [InlineData("--dry-run=true", false, true)]
    [InlineData("--dry-run=false", false, false)]
    [InlineData("", true, true)]
    [InlineData("", false, false)]
    public void DryRunCliOptionTakesPriorityOverFileConfig(string cliInput, bool fileConfigValue, bool expectedValue)
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var fileConfig = new FileConfig { DryRun = fileConfigValue };
        var cliApp = new CommandLineApplication();
        var cliConfig = CliConfig.Create(cliApp);
        cliApp.Parse(string.IsNullOrEmpty(cliInput) ? [] : [cliInput]);
        VersionizeOptions options = ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig);
        options.DryRun.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("--skip-dirty=true", true, true)]
    [InlineData("--skip-dirty=false", true, false)]
    [InlineData("--skip-dirty", true, true)]
    [InlineData("--skip-dirty=true", false, true)]
    [InlineData("--skip-dirty=false", false, false)]
    [InlineData("", true, true)]
    [InlineData("", false, false)]
    public void SkipDirtyCliOptionTakesPriorityOverFileConfig(string cliInput, bool fileConfigValue, bool expectedValue)
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var fileConfig = new FileConfig { SkipDirty = fileConfigValue };
        var cliApp = new CommandLineApplication();
        var cliConfig = CliConfig.Create(cliApp);
        cliApp.Parse(string.IsNullOrEmpty(cliInput) ? [] : [cliInput]);
        VersionizeOptions options = ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig);
        options.SkipDirty.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("--skip-changelog=true", true, true)]
    [InlineData("--skip-changelog=false", true, false)]
    [InlineData("--skip-changelog", true, true)]
    [InlineData("--skip-changelog=true", false, true)]
    [InlineData("--skip-changelog=false", false, false)]
    [InlineData("", true, true)]
    [InlineData("", false, false)]
    public void SkipChangelogCliOptionTakesPriorityOverFileConfig(string cliInput, bool fileConfigValue, bool expectedValue)
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var fileConfig = new FileConfig { SkipChangelog = fileConfigValue };
        var cliApp = new CommandLineApplication();
        var cliConfig = CliConfig.Create(cliApp);
        cliApp.Parse(string.IsNullOrEmpty(cliInput) ? [] : [cliInput]);
        VersionizeOptions options = ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig);
        options.SkipChangelog.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("-i=true", true, true)]
    [InlineData("-i=false", true, false)]
    [InlineData("-i", true, true)]
    [InlineData("-i=true", false, true)]
    [InlineData("-i=false", false, false)]
    [InlineData("", true, true)]
    [InlineData("", false, false)]
    public void IgnoreInsignificantCliOptionTakesPriorityOverFileConfig(string cliInput, bool fileConfigValue, bool expectedValue)
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var fileConfig = new FileConfig { IgnoreInsignificantCommits = fileConfigValue };
        var cliApp = new CommandLineApplication();
        var cliConfig = CliConfig.Create(cliApp);
        cliApp.Parse(string.IsNullOrEmpty(cliInput) ? [] : [cliInput]);
        VersionizeOptions options = ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig);
        options.IgnoreInsignificantCommits.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("--exit-insignificant-commits=true", true, true)]
    [InlineData("--exit-insignificant-commits=false", true, false)]
    [InlineData("--exit-insignificant-commits", true, true)]
    [InlineData("--exit-insignificant-commits=true", false, true)]
    [InlineData("--exit-insignificant-commits=false", false, false)]
    [InlineData("", true, true)]
    [InlineData("", false, false)]
    public void ExitInsignificantCliOptionTakesPriorityOverFileConfig(string cliInput, bool fileConfigValue, bool expectedValue)
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var fileConfig = new FileConfig { ExitInsignificantCommits = fileConfigValue };
        var cliApp = new CommandLineApplication();
        var cliConfig = CliConfig.Create(cliApp);
        cliApp.Parse(string.IsNullOrEmpty(cliInput) ? [] : [cliInput]);
        VersionizeOptions options = ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig);
        options.ExitInsignificantCommits.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("-a=true", true, true)]
    [InlineData("-a=false", true, false)]
    [InlineData("-a", true, true)]
    [InlineData("-a=true", false, true)]
    [InlineData("-a=false", false, false)]
    [InlineData("", true, true)]
    [InlineData("", false, false)]
    public void AggregatePrereleasesCliOptionTakesPriorityOverFileConfig(string cliInput, bool fileConfigValue, bool expectedValue)
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var fileConfig = new FileConfig { AggregatePrereleases = fileConfigValue };
        var cliApp = new CommandLineApplication();
        var cliConfig = CliConfig.Create(cliApp);
        cliApp.Parse(string.IsNullOrEmpty(cliInput) ? [] : [cliInput]);
        VersionizeOptions options = ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig);
        options.AggregatePrereleases.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("--first-parent-only-commits=true", true, true)]
    [InlineData("--first-parent-only-commits=false", true, false)]
    [InlineData("--first-parent-only-commits", true, true)]
    [InlineData("--first-parent-only-commits=true", false, true)]
    [InlineData("--first-parent-only-commits=false", false, false)]
    [InlineData("", true, true)]
    [InlineData("", false, false)]
    public void FirstParentOnlyCommitsCliOptionTakesPriorityOverFileConfig(string cliInput, bool fileConfigValue, bool expectedValue)
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var fileConfig = new FileConfig { FirstParentOnlyCommits = fileConfigValue };
        var cliApp = new CommandLineApplication();
        var cliConfig = CliConfig.Create(cliApp);
        cliApp.Parse(string.IsNullOrEmpty(cliInput) ? [] : [cliInput]);
        VersionizeOptions options = ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig);
        options.FirstParentOnlyCommits.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("-s=true", true, true)]
    [InlineData("-s=false", true, false)]
    [InlineData("-s", true, true)]
    [InlineData("-s=true", false, true)]
    [InlineData("-s=false", false, false)]
    [InlineData("", true, true)]
    [InlineData("", false, false)]
    public void SignCliOptionTakesPriorityOverFileConfig(string cliInput, bool fileConfigValue, bool expectedValue)
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var fileConfig = new FileConfig { Sign = fileConfigValue };
        var cliApp = new CommandLineApplication();
        var cliConfig = CliConfig.Create(cliApp);
        cliApp.Parse(string.IsNullOrEmpty(cliInput) ? [] : [cliInput]);
        VersionizeOptions options = ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig);
        options.Sign.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData("", null, "v{version}")]
    [InlineData("--tag-template {version}", null, "{version}")]
    [InlineData("", "release-{version}", "release-{version}")]
    [InlineData("--tag-template A{version}", "B{version}", "A{version}")]
    public void ReturnsExpectedTagTemplateForNonMonorepo(string cliInput, string fileConfigValue, string expectedTagTemplate)
    {
        TempProject.CreateCsharpProject(_testSetup.WorkingDirectory);
        var fileConfig = new FileConfig { TagTemplate = fileConfigValue };
        var cliApp = new CommandLineApplication();
        var cliConfig = CliConfig.Create(cliApp);
        cliApp.Parse(string.IsNullOrEmpty(cliInput) ? [] : [cliInput]);
        VersionizeOptions options = ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig);
        options.Project.TagTemplate.ShouldBe(expectedTagTemplate);
    }

    [Fact]
    public void ShouldExitWhenProjectVersionElementContainsInvalidCharacters()
    {
        // Arrange
        var projects = new[]
        {
            new ProjectOptions
            {
                Name = "Project1",
                Path = "project1",
                VersionElement = "File-Version", // hyphen is invalid per validation rule
                Changelog = ChangelogOptions.Default
            }
        };

        var dotnetProjectPath = Path.Combine(_testSetup.WorkingDirectory, "project1");
        TempProject.CreateCsharpProject(dotnetProjectPath);

        var fileConfig = new FileConfig { Projects = projects };
        var cliApp = new CommandLineApplication();
        var cliConfig = CliConfig.Create(cliApp);
        cliApp.Parse("--proj-name Project1");

        // Act/Assert
        Should.Throw<VersionizeException>(() => ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig))
            .Message.ShouldBe(ErrorMessages.InvalidVersionElement("File-Version"));
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
