using LibGit2Sharp;
using Shouldly;
using Xunit;

namespace Versionize.Tests;

public partial class WorkingCopyTests
{
    [Fact]
    public void ShouldTagInitialVersionUsingTagOnly()
    {   
        // Arrange
        CommitAll(_testSetup.Repository); 
        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        
        // Act
        workingCopy.Versionize(new VersionizeOptions
        {
            TagOnly = true
        });

        // Assert
        _testSetup.Repository.Tags.Count().ShouldBe(1);
        _testSetup.Repository.Tags.Select(t => t.FriendlyName).ShouldBe(new[] { "v1.0.0" });
        _testSetup.Repository.Commits.Count().ShouldBe(1);
    }
    
    [Fact]
    public void ShouldTagVersionAfterFeatUsingTagOnly()
    {   
        // Arrange
        CommitAll(_testSetup.Repository);
        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        workingCopy.Versionize(new VersionizeOptions
        {
            TagOnly = true
        });

        new FileCommitter(_testSetup).CommitChange("feat: first feature");
                    
        // Act
        workingCopy.Versionize(new VersionizeOptions
        {
            TagOnly = true
        });

        // Assert
        _testSetup
            .Repository
            .Tags
            .Select(x => x.FriendlyName)
            .ShouldBe(new[] {"v1.0.0", "v1.1.0"});
        
        _testSetup
            .Repository
            .Commits
            .Count()
            .ShouldBe(2);
    }
    
    [Fact]
    public void ShouldTagVersionAfterFixUsingTagOnly()
    {   
        // Arrange
        CommitAll(_testSetup.Repository);
        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        workingCopy.Versionize(new VersionizeOptions
        {
            TagOnly = true
        });

        new FileCommitter(_testSetup).CommitChange("fix: first feature");
                    
        // Act
        workingCopy.Versionize(new VersionizeOptions
        {
            TagOnly = true
        });

        // Assert
        _testSetup
            .Repository
            .Tags
            .Select(x => x.FriendlyName)
            .ShouldBe(new[] {"v1.0.0", "v1.0.1"});
        
        _testSetup
            .Repository
            .Commits
            .Count()
            .ShouldBe(2);
    }
    
    [Fact]
    public void ShouldTagVersionWhenMultipleCommitsInOneVersionUsingTagOnly()
    {   
        // Arrange
        CommitAll(_testSetup.Repository);
        var workingCopy = WorkingCopy.Discover(_testSetup.WorkingDirectory);
        workingCopy.Versionize(new VersionizeOptions
        {
            TagOnly = true
        });

        var fc = new FileCommitter(_testSetup);
        fc.CommitChange("fix: first fix");
        fc.CommitChange("fix: second fix");
        fc.CommitChange("feat: first feature");
                    
        // Act
        workingCopy.Versionize(new VersionizeOptions
        {
            TagOnly = true
        });

        // Assert
        _testSetup
            .Repository
            .Tags
            .Select(x => x.FriendlyName)
            .ShouldBe(new[] {"v1.0.0", "v1.1.0"});
        
        _testSetup
            .Repository
            .Commits
            .Count()
            .ShouldBe(4);
    }
}
