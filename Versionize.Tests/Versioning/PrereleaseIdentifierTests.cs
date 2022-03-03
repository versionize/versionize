using NuGet.Versioning;
using Shouldly;
using Versionize.Versioning;
using Xunit;

namespace Versionize.Tests.Versioning;

public class PrereleaseIdentifierTests
{
    [Fact]
    public void ShouldThrowForPreReleaseIdentifierMissingPrereleaseNumber()
    {
        Should.Throw<InvalidPrereleaseIdentifierException>(() => PrereleaseIdentifier.Parse(SemanticVersion.Parse("2.0.0-alpha")));
    }

    [Fact]
    public void ShouldThrowForPreReleaseIdentifierWithoutNumericNumber()
    {
        Should.Throw<InvalidPrereleaseIdentifierException>(() => PrereleaseIdentifier.Parse(SemanticVersion.Parse("2.0.0-alpha.a")));
    }
}
