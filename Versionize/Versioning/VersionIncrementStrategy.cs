using NuGet.Versioning;
using Versionize.CommandLine;
using Versionize.ConventionalCommits;

namespace Versionize.Versioning;

public sealed class VersionIncrementStrategy(IEnumerable<ConventionalCommit> conventionalCommits, IReadOnlyDictionary<string, string[]>? aliases = null)
{
    private readonly IEnumerable<ConventionalCommit> _conventionalCommits = conventionalCommits;
    private readonly Dictionary<string, HashSet<string>> _aliasLookup =
        aliases?.ToDictionary(
            kvp => kvp.Key.ToLowerInvariant(),
            kvp => new HashSet<string>(kvp.Value.Select(v => v.ToLowerInvariant())))
        ?? [];

    public SemanticVersion NextVersion(
        SemanticVersion version,
        string? prereleaseLabel = null,
        bool insignificantCommitsAffectVersion = true)
    {
        var versionImpact = CalculateVersionImpact(insignificantCommitsAffectVersion);
        var isPrerelease = !string.IsNullOrEmpty(prereleaseLabel);

        var nextVersion = versionImpact switch
        {
            VersionImpact.Patch => new SemanticVersion(version.Major, version.Minor, version.Patch + 1),
            VersionImpact.Minor => new SemanticVersion(version.Major, version.Minor + 1, 0),
            VersionImpact.Major => new SemanticVersion(version.Major + 1, 0, 0),
            VersionImpact.None => version,
            _ => throw new VersionizeException(ErrorMessages.VersionImpactCannotBeHandled(versionImpact.ToString()), 1),
        };

        if (version.IsPrerelease && isPrerelease)
        {
            if (versionImpact == VersionImpact.None)
            {
                return version;
            }

            return IsWithinPrereleaseVersionRange(version, versionImpact)
                ? version.IncrementPrerelease(prereleaseLabel!)
                : nextVersion.AsPrerelease(prereleaseLabel!, 0);
        }
        else if (!version.IsPrerelease && isPrerelease)
        {
            return nextVersion.AsPrerelease(prereleaseLabel!, 0);
        }
        else if (version.IsPrerelease && !isPrerelease)
        {
            return (IsWithinPrereleaseVersionRange(version, versionImpact)
                ? version
                : nextVersion).AsRelease();
        }

        return nextVersion;
    }

    private static bool IsWithinPrereleaseVersionRange(SemanticVersion version, VersionImpact versionImpact)
    {
        return versionImpact switch
        {
            VersionImpact.None => true,
            VersionImpact.Patch => true,
            VersionImpact.Minor => version.Patch == 0,
            VersionImpact.Major => version.Patch == 0 && version.Minor == 0,
            _ => throw new VersionizeException(ErrorMessages.VersionImpactCannotBeHandled(versionImpact.ToString()), 1),
        };
    }

    private VersionImpact CalculateVersionImpact(bool insignificantCommitsAffectVersion)
    {
        var versionImpact = VersionImpact.None;

        foreach (var conventionalCommit in _conventionalCommits)
        {
            if (!string.IsNullOrWhiteSpace(conventionalCommit.Type))
            {
                if (IsType(conventionalCommit, "fix"))
                {
                    versionImpact = MaxVersionImpact(versionImpact, VersionImpact.Patch);
                }
                else if (IsType(conventionalCommit, "feat"))
                {
                    versionImpact = MaxVersionImpact(versionImpact, VersionImpact.Minor);
                }
                else if (insignificantCommitsAffectVersion)
                {
                    versionImpact = MaxVersionImpact(versionImpact, VersionImpact.Patch);
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

    private bool IsType(ConventionalCommit commit, string canonicalType)
    {
        if (string.IsNullOrWhiteSpace(commit.Type))
        {
            return false;
        }

        var typeLower = commit.Type.ToLowerInvariant();
        var canonicalLower = canonicalType.ToLowerInvariant();

        if (typeLower == canonicalLower)
        {
            return true;
        }

        if (_aliasLookup.TryGetValue(canonicalLower, out var aliases))
        {
            return aliases.Contains(typeLower);
        }

        return false;
    }
}

public enum VersionImpact
{
    None = 0,
    Patch = 1,
    Minor = 2,
    Major = 3,
}
