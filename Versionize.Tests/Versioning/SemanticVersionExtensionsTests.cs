using NuGet.Versioning;
using Shouldly;
using Xunit;

namespace Versionize.Versioning;

public class SemanticVersionExtensionsTests
{
    [Fact]
    public void ShouldIncrementPatchVersionForPrereleaseByNumber()
    {
        // Arrange
        var version = SemanticVersion.Parse("2.0.0-alpha.1");

        // Act
        var patchVersion = version.IncrementPatchVersion();

        // Assert
        patchVersion.ShouldBe(SemanticVersion.Parse("2.0.0-alpha.2"));
    }

    [Fact]
    public void ShouldIncrementPatchVersionForStableReleasesByPatchVersion()
    {
        // Arrange
        var version = SemanticVersion.Parse("2.0.0");

        // Act
        var patchVersion = version.IncrementPatchVersion();

        // Assert
        patchVersion.ShouldBe(SemanticVersion.Parse("2.0.1"));
    }

    [Fact]
    public void IncrementPrerelease_ShouldResetNumber_When_LabelIsDifferent()
    {
        // Arrange
        var version = SemanticVersion.Parse("2.0.0-alpha.1");

        // Act
        var newVersion = version.IncrementPrerelease("beta");

        // Assert
        newVersion.ShouldBe(SemanticVersion.Parse("2.0.0-beta.0"));
    }

    [Fact]
    public void IncrementPrerelease_ShouldIncrementNumber_When_LabelIsSame()
    {
        // Arrange
        var version = SemanticVersion.Parse("2.0.0-alpha.1");

        // Act
        var newVersion = version.IncrementPrerelease("alpha");

        // Assert
        newVersion.ShouldBe(SemanticVersion.Parse("2.0.0-alpha.2"));
    }

    [Fact]
    public void AsRelease_ShouldReturnVersionWithoutPrerelease()
    {
        // Arrange
        var version = SemanticVersion.Parse("2.0.0-alpha.1");

        // Act
        var releaseVersion = version.AsRelease();

        // Assert
        releaseVersion.ShouldBe(SemanticVersion.Parse("2.0.0"));
    }
}
