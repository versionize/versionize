using NuGet.Versioning;
using Shouldly;
using Xunit;

namespace Versionize.Config;

public class ProjectOptionsTests
{
    [Theory]
    [InlineData("{name}/v{version}", false, "1.2.3", "MyProject/v1.2.3", true)]
    [InlineData("{name}/v{version}", false, "1.2.3", "MyProject/v1.2.4", false)]
    [InlineData("{name}/v{version}", true, "1.2.3", "MyProject/v1.2.3", true)]
    [InlineData("{name}/v{version}", true, "1.2.3", "MyProject/1.2.3", true)]
    [InlineData("{name}/v{version}", true, "1.2.3", "MyProject/v1.2.4", false)]
    [InlineData("{name}/v{version}", true, "1.2.3", "MyProject/1.2.4", false)]
    [InlineData("v{version}", false, "1.2.3", "v1.2.3", true)]
    [InlineData("v{version}", false, "1.2.3", "1.2.3", true)]
    [InlineData("v{version}", true, "1.2.3", "v1.2.3", true)]
    [InlineData("v{version}", true, "1.2.3", "1.2.3", true)]
    [InlineData("v{version}", true, "1.2.3", "v1.2.4", false)]
    [InlineData("v{version}", true, "1.2.3", "1.2.4", false)]
    public void HasMatchingTag(string tagTemplate, bool omitTagVersionPrefix, string versionString, string tagName, bool expectedResult)
    {
        // Arrange
        var projectOptions = new ProjectOptions { TagTemplate = tagTemplate, OmitTagVersionPrefix = omitTagVersionPrefix };
        if (tagTemplate.Contains("{name}"))
        {
            projectOptions.Name = tagName[..tagName.IndexOf('/')];
        }
        var version = SemanticVersion.Parse(versionString);

        // Act
        var result = projectOptions.HasMatchingTag(version, tagName);

        // Assert
        result.ShouldBe(expectedResult);
    }
    
    [Theory]
    [InlineData("{name}/v{version}", false, "MyProject", "1.2.3", "MyProject/v1.2.3")]
    [InlineData("{name}/v{version}", true, "MyProject", "1.2.3", "MyProject/1.2.3")]
    [InlineData("v{version}", false, "", "1.2.3", "v1.2.3")]
    [InlineData("v{version}", true, "", "1.2.3", "1.2.3")]
    public void GetTagName_BySemanticVersion(string tagTemplate, bool omitTagVersionPrefix, string name, string versionString, string expectedTagName)
    {
        // Arrange
        var projectOptions = new ProjectOptions { TagTemplate = tagTemplate, OmitTagVersionPrefix = omitTagVersionPrefix, Name = name };
        var version = SemanticVersion.Parse(versionString);
        
        // Act
        var tagName = projectOptions.GetTagName(version);

        // Assert
        tagName.ShouldBe(expectedTagName);
    }
    
    [Theory]
    [InlineData("{name}/v{version}", false, "MyProject", "1.2.3", "MyProject/v1.2.3")]
    [InlineData("{name}/v{version}", true, "MyProject", "1.2.3", "MyProject/1.2.3")]
    [InlineData("v{version}", false, "", "1.2.3", "v1.2.3")]
    [InlineData("v{version}", true, "", "1.2.3", "1.2.3")]
    public void GetTagName_ByVersionString(string tagTemplate, bool omitTagVersionPrefix, string name, string versionString, string expectedTagName)
    {
        // Arrange
        var projectOptions = new ProjectOptions { TagTemplate = tagTemplate, OmitTagVersionPrefix = omitTagVersionPrefix, Name = name };
        
        // Act
        var tagName = projectOptions.GetTagName(versionString);

        // Assert
        tagName.ShouldBe(expectedTagName);
    }
}
