using NuGet.Versioning;
using Shouldly;
using Versionize.CommandLine;
using Xunit;

namespace Versionize.Versioning;

public class PrereleaseIdentifierTests
{
    [Fact]
    public void ShouldThrowForPreReleaseIdentifierMissingPrereleaseNumber()
    {
        Should.Throw<VersionizeException>(() => PrereleaseIdentifier.Parse(SemanticVersion.Parse("2.0.0-alpha")));
    }

    [Fact]
    public void ShouldThrowForPreReleaseIdentifierWithoutNumericNumber()
    {
        Should.Throw<VersionizeException>(() => PrereleaseIdentifier.Parse(SemanticVersion.Parse("2.0.0-alpha.a")));
    }
}
