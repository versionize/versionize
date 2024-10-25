using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
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
        //var json = JsonConvert.SerializeObject(fileConfig);
        //File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, ".versionize"), json);
        var cliConfig = CliConfig.Create(new CommandLineApplication());

        Should.Throw<CommandLineExitException>(() => ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig));
        _testPlatformAbstractions.Messages[0].ShouldBe("Two or more projects have changelog paths pointing to the same location.");
    }

    [Fact]
    public void DefaultProjectTypeShouldBeDotnet()
    {
        var app = new CommandLineApplication();
        var cliConfig = CliConfig.Create(app);
        app.Parse([]);
        var config = ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig: null);
        config.ProjectType.ShouldBe(ProjectType.Dotnet);
    }

    [Theory]
    [InlineData("--project-type dotnet", ProjectType.Dotnet)]
    [InlineData("--project-type unity", ProjectType.Unity)]
    [InlineData("--project-type none", ProjectType.None)]
    public void ProjectTypeShouldBeDotnet(string input, ProjectType expectedProjectType)
    {
        var app = new CommandLineApplication();
        var cliConfig = CliConfig.Create(app);
        app.Parse(input);
        var config = ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig: null);
        config.ProjectType.ShouldBe(expectedProjectType);
    }

    [Theory]
    [InlineData("--project-type dotnet", "unity", ProjectType.Dotnet)]
    [InlineData("--project-type unity", "dotnet", ProjectType.Unity)]
    [InlineData("--project-type none", "dotnet", ProjectType.None)]
    public void CliConfigHasPriorityOverFileConfig(string cliInput, string fileInput, ProjectType expectedProjectType)
    {
        var app = new CommandLineApplication();
        var cliConfig = CliConfig.Create(app);
        app.Parse(cliInput);
        var fileConfig = new FileConfig { ProjectType = fileInput };
        var config = ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig, fileConfig);
        config.ProjectType.ShouldBe(expectedProjectType);
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
