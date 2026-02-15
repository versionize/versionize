using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

using Shouldly;
using Versionize.CommandLine;
using Versionize.Tests.TestSupport;
using Xunit;

namespace Versionize.Config;

public class FileConfigLoaderTests : IDisposable
{
    private readonly TestSetup _testSetup;
    private readonly TestPlatformAbstractions _testPlatformAbstractions;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
    private readonly Func<HttpClient> _originalHttpClientFactory;

    public FileConfigLoaderTests()
    {
        _testSetup = TestSetup.Create();
        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
        _originalHttpClientFactory = FileConfigLoader.HttpClientFactory;
    }

    [Fact]
    public void ShouldMergeExtendedConfigWithLocalOverrides()
    {
        var configDir = Path.Combine(_testSetup.WorkingDirectory, "config");
        Directory.CreateDirectory(configDir);

        var sharedPath = Path.Combine(configDir, "shared.versionize");
        var localPath = Path.Combine(configDir, ".versionize");

        var sharedConfig = new FileConfig
        {
            SkipDirty = true,
            CommitSuffix = "[skip ci]",
            Projects =
            [
                new ProjectOptions { Name = "App", Path = "shared-app", TagTemplate = "v{version}" }
            ]
        };

        var localConfig = new FileConfig
        {
            Extends = "shared.versionize",
            SkipCommit = true,
            Projects =
            [
                new ProjectOptions { Name = "App", Path = "local-app", TagTemplate = "v{version}" }
            ]
        };

        File.WriteAllText(sharedPath, Serialize(sharedConfig));
        File.WriteAllText(localPath, Serialize(localConfig));

        var config = FileConfigLoader.Load(localPath);

        config.ShouldNotBeNull();
        config.SkipDirty.ShouldNotBeNull().ShouldBeTrue();
        config.SkipCommit.ShouldNotBeNull().ShouldBeTrue();
        config.CommitSuffix.ShouldBe("[skip ci]");
        config.Projects.Length.ShouldBe(1);
        config.Projects[0].Path.ShouldBe("local-app");
    }

    [Fact]
    public void ShouldMergeProjectsByName()
    {
        var configDir = Path.Combine(_testSetup.WorkingDirectory, "config");
        Directory.CreateDirectory(configDir);

        var sharedPath = Path.Combine(configDir, "shared.versionize");
        var localPath = Path.Combine(configDir, ".versionize");

        var sharedConfig = new FileConfig
        {
            Projects =
            [
                new ProjectOptions { Name = "App", Path = "shared-app", TagTemplate = "v{version}" },
                new ProjectOptions { Name = "Lib", Path = "shared-lib", TagTemplate = "v{version}" }
            ]
        };

        var localConfig = new FileConfig
        {
            Extends = "shared.versionize",
            Projects =
            [
                new ProjectOptions { Name = "App", Path = "local-app", TagTemplate = "v{version}" }
            ]
        };

        File.WriteAllText(sharedPath, Serialize(sharedConfig));
        File.WriteAllText(localPath, Serialize(localConfig));

        var config = FileConfigLoader.Load(localPath);

        config.ShouldNotBeNull();
        config!.Projects.Length.ShouldBe(2);
        config.Projects.Single(p => p.Name == "App").Path.ShouldBe("local-app");
        config.Projects.Single(p => p.Name == "Lib").Path.ShouldBe("shared-lib");
    }

    [Fact]
    public void ShouldLoadExtendedConfigFromHttps()
    {
        var configDir = Path.Combine(_testSetup.WorkingDirectory, "config");
        Directory.CreateDirectory(configDir);

        var localPath = Path.Combine(configDir, ".versionize");
        var sharedUri = new Uri("https://example.test/shared/.versionize");

        var sharedConfig = new FileConfig
        {
            SkipDirty = true,
            CommitSuffix = "[skip ci]",
            Projects =
            [
                new ProjectOptions { Name = "App", Path = "shared-app", TagTemplate = "v{version}" }
            ]
        };

        var localConfig = new FileConfig
        {
            Extends = sharedUri.AbsoluteUri,
            SkipCommit = true,
            Projects =
            [
                new ProjectOptions { Name = "App", Path = "local-app", TagTemplate = "v{version}" }
            ]
        };

        File.WriteAllText(localPath, Serialize(localConfig));

        var body = Serialize(sharedConfig);

        FileConfigLoader.HttpClientFactory = () => new HttpClient(new StubHttpMessageHandler(request =>
        {
            var requestUri = request.RequestUri?.AbsoluteUri ?? string.Empty;
            sharedUri.AbsoluteUri.ShouldBe(requestUri);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            };
        }));

        var config = FileConfigLoader.Load(localPath);

        config.ShouldNotBeNull();
        config!.SkipDirty.ShouldNotBeNull().ShouldBeTrue();
        config.SkipCommit.ShouldNotBeNull().ShouldBeTrue();
        config.CommitSuffix.ShouldBe("[skip ci]");
        config.Projects.Single(p => p.Name == "App").Path.ShouldBe("local-app");
    }

    [Fact]
    public void ShouldExitWhenHttpsConfigIsNotReachable()
    {
        var configDir = Path.Combine(_testSetup.WorkingDirectory, "config");
        Directory.CreateDirectory(configDir);

        var localPath = Path.Combine(configDir, ".versionize");
        var sharedUri = new Uri("https://example.test/missing/.versionize");

        var localConfig = new FileConfig
        {
            Extends = sharedUri.AbsoluteUri
        };

        File.WriteAllText(localPath, Serialize(localConfig));

        FileConfigLoader.HttpClientFactory = () => new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)));

        Should.Throw<CommandLineExitException>(() => FileConfigLoader.Load(localPath));
        _testPlatformAbstractions.Messages.Last().ShouldContain("Failed to fetch config from");
    }

    private string Serialize(FileConfig config)
    {
        return JsonSerializer.Serialize(config, _jsonSerializerOptions);
    }

    public void Dispose()
    {
        FileConfigLoader.HttpClientFactory = _originalHttpClientFactory;
        _testSetup.Dispose();
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }
}
