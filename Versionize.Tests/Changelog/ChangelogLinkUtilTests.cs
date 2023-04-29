using NuGet.Versioning;
using Xunit;

namespace Versionize.Changelog.Tests;

public class ChangelogLinkUtilTests
{
    [Fact]
    public void ShouldGenerateExpectedUrl()
    {
        var compareUrlFormat = "https://www.github.com/{{owner}}/{{repository}}/compare/{{previousTag}}...{{currentTag}}";
        string organization = "myOrg";
        string repository = "myRepo";
        SemanticVersion newVersion = SemanticVersion.Parse("1.2.3");
        SemanticVersion previousVersion = SemanticVersion.Parse("1.2.2");

        var actual = ChangelogLinkUtil.CreateCompareUrl(
            compareUrlFormat,
            organization,
            repository,
            newVersion,
            previousVersion);

        var expected = "https://www.github.com/myOrg/myRepo/compare/v1.2.2...v1.2.3";

        Assert.Equal(expected, actual);
    }
}
