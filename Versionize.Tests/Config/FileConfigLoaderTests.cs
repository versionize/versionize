using System.Net;
using System.Text.Json;
using Shouldly;
using Versionize.CommandLine;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Config;

public sealed class FileConfigLoaderTests : IDisposable
{
    private readonly TestSetup _testSetup;

    private static readonly JsonSerializerOptions serializerConfig = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public FileConfigLoaderTests()
    {
        _testSetup = TestSetup.Create();
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
    public void ShouldMergeExtendedConfigFromUrl()
    {
        var baseUrl = "https://example.com/config/base.versionize";
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

        var localConfigPath = Path.Join(_testSetup.WorkingDirectory, ".versionize");
        var localConfig = new FileConfig
        {
            Extends = baseUrl,
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

        var httpClient = CreateHttpClient(new Dictionary<string, string>
        {
            [baseUrl] = SerializeConfig(baseConfig)
        });

        var merged = FileConfigLoader.LoadMerged(localConfigPath, httpClient);

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
    public void ShouldResolveRelativeExtendsFromUrl()
    {
        var baseUrl = "https://example.com/config/base.versionize";
        var sharedUrl = "https://example.com/config/shared.versionize";

        var sharedConfig = new FileConfig
        {
            SkipDirty = true,
            CommitSuffix = "[shared]"
        };

        var baseConfig = new FileConfig
        {
            Extends = "shared.versionize",
            CommitSuffix = "[base]"
        };

        var localConfigPath = Path.Join(_testSetup.WorkingDirectory, ".versionize");
        var localConfig = new FileConfig
        {
            Extends = baseUrl,
            SkipDirty = false
        };
        File.WriteAllText(localConfigPath, SerializeConfig(localConfig));

        var httpClient = CreateHttpClient(new Dictionary<string, string>
        {
            [baseUrl] = SerializeConfig(baseConfig),
            [sharedUrl] = SerializeConfig(sharedConfig)
        });

        var merged = FileConfigLoader.LoadMerged(localConfigPath, httpClient);

        merged.ShouldNotBeNull();
        merged.SkipDirty.ShouldNotBeNull().ShouldBeFalse();
        merged.CommitSuffix.ShouldNotBeNull().ShouldBe("[base]");
    }

    [Fact]
    public void ShouldFailWhenExtendedFileIsMissing()
    {
        var localConfigPath = Path.Join(_testSetup.WorkingDirectory, ".versionize");
        var localConfig = new FileConfig
        {
            Extends = "missing.versionize",
            SkipDirty = true
        };
        File.WriteAllText(localConfigPath, SerializeConfig(localConfig));

        var exception = Should.Throw<VersionizeException>(() => FileConfigLoader.LoadMerged(localConfigPath));
        var missingPath = Path.GetFullPath(Path.Join(_testSetup.WorkingDirectory, "missing.versionize"));
        exception.Message.ShouldBe(ErrorMessages.ExtendedConfigFileNotFound(missingPath));
    }

    [Fact]
    public void ShouldFailWhenExtendedFileIsInvalidJson()
    {
        var invalidConfigPath = Path.Join(_testSetup.WorkingDirectory, "invalid.versionize");
        File.WriteAllText(invalidConfigPath, "{ not: valid json");

        var localConfigPath = Path.Join(_testSetup.WorkingDirectory, ".versionize");
        var localConfig = new FileConfig
        {
            Extends = "invalid.versionize",
            SkipDirty = true
        };
        File.WriteAllText(localConfigPath, SerializeConfig(localConfig));

        var exception = Should.Throw<VersionizeException>(() => FileConfigLoader.LoadMerged(localConfigPath));
        exception.Message.ShouldStartWith(ErrorMessages.FailedToParseVersionizeFile(string.Empty).TrimEnd());
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }

    private static string SerializeConfig(FileConfig config)
    {
        return JsonSerializer.Serialize(config, serializerConfig);
    }

    private static HttpClient CreateHttpClient(Dictionary<string, string> responses)
    {
        var handler = new TestHttpMessageHandler(responses);
        return new HttpClient(handler);
    }

    private class TestHttpMessageHandler(Dictionary<string, string> responses) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.ToString() ?? string.Empty;
            if (!responses.TryGetValue(uri, out var content))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };

            return Task.FromResult(response);
        }
    }
}
