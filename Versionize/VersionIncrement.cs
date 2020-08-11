using System;
using System.Collections.Generic;
using System.Linq;

namespace Versionize
{
    public class VersionIncrementStrategy
    {
        private readonly VersionImpact _versionImpact;

        private VersionIncrementStrategy(VersionImpact versionImpact)
        {
            _versionImpact = versionImpact;
        }

        public Version NextVersion(Version version, bool ignoreInsignificant)
        {
            switch (_versionImpact)
            {
                case VersionImpact.patch:
                    return new Version(version.Major, version.Minor, version.Build + 1);
                case VersionImpact.minor:
                    return new Version(version.Major, version.Minor + 1, 0);
                case VersionImpact.major:
                    return new Version(version.Major + 1, 0, 0);
                case VersionImpact.none:
                    var buildVersion = ignoreInsignificant ? version.Build : version.Build + 1;
                    return new Version(version.Major, version.Minor, buildVersion);
                default:
                    throw new InvalidOperationException($"Version impact of {_versionImpact} cannot be handled");
            }
        }

        public static VersionIncrementStrategy CreateFrom(List<ConventionalCommit> conventionalCommits)
        {
            // TODO: Quick and dirty implementation - Conventions? Better comparison?
            var versionImpact = VersionImpact.none;

            foreach (var conventionalCommit in conventionalCommits)
            {
                if (!String.IsNullOrWhiteSpace(conventionalCommit.Type))
                {
                    switch (conventionalCommit.Type)
                    {
                        case "fix":
                            versionImpact = MaxVersionImpact(versionImpact, VersionImpact.patch);
                            break;
                        case "feat":
                            versionImpact = MaxVersionImpact(versionImpact, VersionImpact.minor);
                            break;
                        default:
                            break;
                    }
                }

                if (conventionalCommit.Notes.Any(note => "BREAKING CHANGE".Equals(note.Title, StringComparison.InvariantCultureIgnoreCase)))
                {
                    versionImpact = MaxVersionImpact(versionImpact, VersionImpact.major);
                }
            }

            return new VersionIncrementStrategy(versionImpact);
        }

        private static VersionImpact MaxVersionImpact(VersionImpact impact1, VersionImpact impact2)
        {
            return (VersionImpact)Math.Max((int)impact1, (int)impact2);
        }
    }

    public enum VersionImpact
    {
        none = 0,

        patch = 1,
        minor = 2,
        major = 3,
    }
}
