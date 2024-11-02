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

        Should.Throw<CommandLineExitException>(() => ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig));
        _testPlatformAbstractions.Messages[0].ShouldBe("Two or more projects have changelog paths pointing to the same location.");
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

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
