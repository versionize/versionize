using System;
using System.Collections.Generic;
using NuGet.Versioning;

namespace Versionize
{
    public class VersionIncrementStrategy
    {
        private readonly IEnumerable<ConventionalCommit> _conventionalCommits;

        public VersionIncrementStrategy(IEnumerable<ConventionalCommit> conventionalCommits)
        {
            _conventionalCommits = conventionalCommits;
        }

        public SemanticVersion NextVersion(SemanticVersion version, string preReleaseLabel = null)
        {
            var versionImpact = CalculateVersionImpact();

            var nextVersion = versionImpact switch
            {
                VersionImpact.Patch => new SemanticVersion(version.Major, version.Minor, version.Patch + 1),
                VersionImpact.Minor => new SemanticVersion(version.Major, version.Minor + 1, 0),
                VersionImpact.Major => new SemanticVersion(version.Major + 1, 0, 0),
                VersionImpact.None => version,
                _ => throw new InvalidOperationException($"Version impact of {versionImpact} cannot be handled"),
            };

            if (!string.IsNullOrWhiteSpace(preReleaseLabel))
            {
                return nextVersion.WithPreRelease(preReleaseLabel, 0);
            }
            else
            {
                return nextVersion;
            }
        }

        private VersionImpact CalculateVersionImpact()
        {
            // TODO: Quick and dirty implementation - Conventions? Better comparison?
            var versionImpact = VersionImpact.None;

            foreach (var conventionalCommit in _conventionalCommits)
            {
                if (!string.IsNullOrWhiteSpace(conventionalCommit.Type))
                {
                    if (conventionalCommit.IsFix)
                    {
                        versionImpact = MaxVersionImpact(versionImpact, VersionImpact.Patch);
                    }
                    else if (conventionalCommit.IsFeature)
                    {
                        versionImpact = MaxVersionImpact(versionImpact, VersionImpact.Minor);
                    }
                }

                if (conventionalCommit.IsBreakingChange)
                {
                    versionImpact = MaxVersionImpact(versionImpact, VersionImpact.Major);
                }
            }

            return versionImpact;
        }

        private static VersionImpact MaxVersionImpact(VersionImpact impact1, VersionImpact impact2)
        {
            return (VersionImpact)Math.Max((int)impact1, (int)impact2);
        }
    }

    public static class SemanticVersionExtensions
    {
        public static SemanticVersion WithPreRelease(this SemanticVersion version, string preReleaseLabel, int preReleaseNumber)
        {
            return new SemanticVersion(version.Major, version.Minor, version.Patch, new[] { preReleaseLabel, preReleaseNumber.ToString() }, null);
        }
    }

    public enum VersionImpact
    {
        None = 0,

        Patch = 1,
        Minor = 2,
        Major = 3,
    }
}
