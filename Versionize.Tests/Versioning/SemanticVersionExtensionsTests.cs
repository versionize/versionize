using NuGet.Versioning;
using Shouldly;
using Xunit;

namespace Versionize.Versioning;

public class SemanticVersionExtensionsTests
{
    [Fact]
    public void ShouldIncrementPatchVersionForPrereleaseByNumber()
    {
        var version = SemanticVersion.Parse("2.0.0-alpha.1");
        var patchVersion = version.IncrementPatchVersion();

        patchVersion.ShouldBe(SemanticVersion.Parse("2.0.0-alpha.2"));
    }

    [Fact]
    public void ShouldIncrementPatchVersionForStableReleasesByPatchVersion()
    {
        var version = SemanticVersion.Parse("2.0.0");
        var patchVersion = version.IncrementPatchVersion();

        patchVersion.ShouldBe(SemanticVersion.Parse("2.0.1"));
    }
}
