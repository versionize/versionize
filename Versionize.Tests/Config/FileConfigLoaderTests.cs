using System.Text.Json;
using Shouldly;
using Versionize.CommandLine;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Config;

public sealed class FileConfigLoaderTests : IDisposable
{
    private readonly TestSetup _testSetup;
    private readonly TestPlatformAbstractions _testPlatformAbstractions;

    private static readonly JsonSerializerOptions serializerConfig = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public FileConfigLoaderTests()
    {
        _testSetup = TestSetup.Create();
        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
    }

    [Fact]
    public void ShouldMergeExtendedConfigFromFile()
    {
        var baseConfigPath = Path.Join(_testSetup.WorkingDirectory, "base.versionize");
        var baseConfig = new FileConfig
        {
            SkipDirty = true,
            CommitSuffix = "[skip ci]",
            Projects =
          [
            new ProjectOptions
          {
            Name = "ProjectA",
            Path = "src/A"
          }
          ],
            Changelog = new ChangelogOptions
            {
                Header = "Base header"
            }
        };
        File.WriteAllText(baseConfigPath, SerializeConfig(baseConfig));

        var localConfigPath = Path.Join(_testSetup.WorkingDirectory, ".versionize");
        var localConfig = new FileConfig
        {
            Extends = "base.versionize",
            SkipDirty = false,
            Projects =
          [
            new ProjectOptions
          {
            Name = "ProjectB",
            Path = "src/B"
          }
          ],
            Changelog = new ChangelogOptions
            {
                Header = "Local header"
            }
        };
        File.WriteAllText(localConfigPath, SerializeConfig(localConfig));

        var merged = FileConfigLoader.LoadMerged(localConfigPath);

        merged.ShouldNotBeNull();
        merged.SkipDirty.ShouldNotBeNull().ShouldBeFalse();
        merged.CommitSuffix.ShouldNotBeNull().ShouldBe("[skip ci]");
        merged.Changelog?.Header.ShouldNotBeNull().ShouldBe("Local header");
        merged.Projects.ShouldNotBeNull();
        merged.Projects!.Length.ShouldBe(2);
        merged.Projects[0].Name.ShouldBe("ProjectA");
        merged.Projects[1].Name.ShouldBe("ProjectB");
    }

    [Fact]
    public void ShouldDetectCyclicExtends()
    {
        var configAPath = Path.Join(_testSetup.WorkingDirectory, "a.versionize");
        var configBPath = Path.Join(_testSetup.WorkingDirectory, "b.versionize");

        var configA = new FileConfig
        {
            Extends = "b.versionize",
            SkipDirty = true
        };
        File.WriteAllText(configAPath, SerializeConfig(configA));

        var configB = new FileConfig
        {
            Extends = "a.versionize",
            SkipDirty = false
        };
        File.WriteAllText(configBPath, SerializeConfig(configB));

        var exception = Should.Throw<VersionizeException>(() => FileConfigLoader.LoadMerged(configAPath));
        exception.Message.ShouldBe(ErrorMessages.ExtendedConfigCycleDetected(Path.GetFullPath(configAPath)));
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }

    private static string SerializeConfig(FileConfig config)
    {
        return JsonSerializer.Serialize(config, serializerConfig);
    }
}
