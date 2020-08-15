using System;
using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace Versionize.Tests
{
    public class VersionIncrementStrategyTests
    {
        [Fact]
        public void ShouldIncrementPatchVersionForEmptyCommits()
        {
            var strategy = VersionIncrementStrategy.CreateFrom(new List<ConventionalCommit>());
            strategy.NextVersion(new Version(1, 1, 1))
                .ShouldBe(new Version(1, 1, 2));
        }

        [Fact]
        public void ShouldNotIncrementPatchVersionForEmptyCommitsIfIgnoreInsignificantIsGiven()
        {
            var strategy = VersionIncrementStrategy.CreateFrom(new List<ConventionalCommit>());
            strategy.NextVersion(new Version(1, 1, 1), true)
                .ShouldBe(new Version(1, 1, 1));
        }
        
        [Fact]
        public void ShouldNotIncrementPatchVersionForInsignificantCommitsIfIgnoreInsignificantIsGiven()
        {
            var strategy = VersionIncrementStrategy.CreateFrom(new List<ConventionalCommit>()
            {
                new ConventionalCommit() { Type = "chore"}
            });
            
            strategy.NextVersion(new Version(1, 1, 1), true)
                .ShouldBe(new Version(1, 1, 1));
        }
        
        [Fact]
        public void ShouldIncrementPatchVersionForFixCommitsIfIgnoreInsignificantIsGiven()
        {
            var strategy = VersionIncrementStrategy.CreateFrom(new List<ConventionalCommit>()
            {
                new ConventionalCommit() { Type = "fix"}
            });
            
            strategy.NextVersion(new Version(1, 1, 1), true)
                .ShouldBe(new Version(1, 1, 2));
        }
        
        [Fact]
        public void ShouldIncrementMinorVersionForFeatures()
        {
            var strategy = VersionIncrementStrategy.CreateFrom(new List<ConventionalCommit>()
            {
                new ConventionalCommit()
                {
                    Type = "feat"
                }
            });
            
            strategy.NextVersion(new Version(1, 1, 1))
                .ShouldBe(new Version(1, 2, 0));
        }
        
        [Fact]
        public void ShouldIncrementMajorVersionForBreakingChanges()
        {
            var strategy = VersionIncrementStrategy.CreateFrom(new List<ConventionalCommit>()
            {
                new ConventionalCommit()
                {
                    Type = "chore",
                    Notes = new List<ConventionalCommitNote>()
                    {
                        new ConventionalCommitNote() { Title = "BREAKING CHANGE"}
                    }
                }
            });
            
            strategy.NextVersion(new Version(1, 1, 1))
                .ShouldBe(new Version(2, 0, 0));
        }
    }
}
