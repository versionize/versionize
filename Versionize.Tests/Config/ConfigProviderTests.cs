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

        var config = new FileConfig
        {
            SkipDirty = true,
            Projects = projects
        };
        var json = JsonConvert.SerializeObject(config);
        File.WriteAllText(Path.Join(_testSetup.WorkingDirectory, ".versionize"), json);
        var cliConfig = CliConfig.Create(new CommandLineApplication());

        Should.Throw<CommandLineExitException>(() => ConfigProvider.GetSelectedOptions(_testSetup.WorkingDirectory, cliConfig));
        _testPlatformAbstractions.Messages[0].ShouldBe("Two or more projects have changelog paths pointing to the same location.");
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
