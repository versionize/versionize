using Xunit;
using Shouldly;

namespace Versionize.Git;

public class ReleaseTagParserTests
{
    [Theory]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("v1.2.3-alpha.1", "1.2.3-alpha.1")]
    [InlineData("release-1.2.3-beta.2+build.456", "1.2.3-beta.2+build.456")]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("no version here", "")]
    public void ShouldExtractSemanticVersion(string input, string expected)
    {
        var result = ReleaseTagParser.ExtractVersion(input);
        result.ShouldBe(expected);
    }
}
