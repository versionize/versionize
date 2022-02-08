using System;
using System.Collections.Generic;
using NuGet.Versioning;
using Versionize.Versioning;

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
            var isPrerelease = !string.IsNullOrEmpty(preReleaseLabel);

            var nextVersion = versionImpact switch
            {
                VersionImpact.Patch => new SemanticVersion(version.Major, version.Minor, version.Patch + 1),
                VersionImpact.Minor => new SemanticVersion(version.Major, version.Minor + 1, 0),
                VersionImpact.Major => new SemanticVersion(version.Major + 1, 0, 0),
                VersionImpact.None => version,
                _ => throw new InvalidOperationException($"Version impact of {versionImpact} cannot be handled"),
            };

            if (version.IsPrerelease && isPrerelease)
            {
                if (versionImpact == VersionImpact.None)
                {
                    return version;
                }

                return IsWithinPrereleaseVersionRange(version, versionImpact)?version.IncrementPreRelease(preReleaseLabel):nextVersion.AsPreRelease(preReleaseLabel, 0);
            }
            else if (!version.IsPrerelease && isPrerelease)
            {
                return nextVersion.AsPreRelease(preReleaseLabel, 0);
            }
            else if (version.IsPrerelease && !isPrerelease)
            {
                return (IsWithinPrereleaseVersionRange(version, versionImpact)?version:nextVersion).AsRelease();
            }
            else
            {
                return nextVersion;
            }
        }

        private bool IsWithinPrereleaseVersionRange(SemanticVersion version, VersionImpact versionImpact)  {
            return versionImpact switch {
                VersionImpact.None => true,
                VersionImpact.Patch => true,
                VersionImpact.Minor => version.Patch == 0,
                VersionImpact.Major => version.Patch == 0 && version.Minor == 0,
                _ => throw new InvalidOperationException($"Version impact of {versionImpact} cannot be handled"),
            };
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

    public enum VersionImpact
    {
        None = 0,

        Patch = 1,
        Minor = 2,
        Major = 3,
    }
}
