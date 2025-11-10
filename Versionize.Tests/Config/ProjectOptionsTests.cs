using Shouldly;
using Xunit;
using LibGit2Sharp;
using NSubstitute;

namespace Versionize.Config;

public sealed class ProjectOptionsTests
{
    [Theory]
    [InlineData("v{version}", "v1.2.3", "1.2.3")]
    [InlineData("v{version}", "v1.2.3-alpha.1", "1.2.3-alpha.1")]
    [InlineData("v{version}", "v1.2.3-alpha.1+build.123", "1.2.3-alpha.1+build.123")]
    [InlineData("{version}-release", "1.2.3-release", "1.2.3")]
    [InlineData("{version}-release", "1.2.3-alpha.1-release", "1.2.3-alpha.1")]
    [InlineData("release-{version}-final", "release-1.2.3-final", "1.2.3")]
    [InlineData("release-{version}-final", "release-2.0.0-beta.1-final", "2.0.0-beta.1")]
    [InlineData("{name}/v{version}", "myproject/v1.2.3", "1.2.3")]
    [InlineData("{name}/v{version}", "myproject/v1.2.3-rc.1", "1.2.3-rc.1")]
    [InlineData("{name}-v{version}", "myproject-v1.2.3", "1.2.3")]
    [InlineData("v{version}-{name}", "v1.2.3-myproject", "1.2.3")]
    public void ShouldExtractVersionFromTagWithPrefixesAndSuffixes(
        string tagTemplate,
        string tagName,
        string expectedVersion)
    {
        // Arrange
        var projectOptions = new ProjectOptions
        {
            Name = "myproject",
            TagTemplate = tagTemplate
        };

        var tag = Substitute.For<Tag>();
        tag.FriendlyName.Returns(tagName);

        // Act
        var version = projectOptions.ExtractTagVersion(tag);

        var v1 = version.ToString();
        var v2 = version.ToFullString();

        // Assert
        version.ShouldNotBeNull();
        version.ToFullString().ShouldBe(expectedVersion);
    }

    [Fact]
    public void ShouldReturnNullWhenTagTemplateDoesNotContainVersionPlaceholder()
    {
        // Arrange
        var projectOptions = new ProjectOptions
        {
            Name = "myproject",
            TagTemplate = "static-tag-name"
        };

        var tag = Substitute.For<Tag>();
        tag.FriendlyName.Returns("static-tag-name");

        // Act
        var version = projectOptions.ExtractTagVersion(tag);

        // Assert
        version.ShouldBeNull();
    }
}
