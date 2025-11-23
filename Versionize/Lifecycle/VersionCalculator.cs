using NuGet.Versioning;
using Versionize.Config;
using Versionize.ConventionalCommits;
using Versionize.Versioning;
using Versionize.CommandLine;

namespace Versionize.Lifecycle;

public sealed class VersionCalculator
{
    public static SemanticVersion Bump(
        Options options,
        SemanticVersion? version,
        bool isInitialRelease,
        IReadOnlyList<ConventionalCommit> conventionalCommits)
    {
        var versionIncrement = new VersionIncrementStrategy(conventionalCommits);

        var insignificantCommitsAffectVersion = !(options.IgnoreInsignificantCommits || options.ExitInsignificantCommits);
        SemanticVersion nextVersion = isInitialRelease || version is null
            ? version ?? new SemanticVersion(1, 0, 0)
            : versionIncrement.NextVersion(version, options.Prerelease, insignificantCommitsAffectVersion);

        if (!isInitialRelease && nextVersion == version)
        {
            if (options.IgnoreInsignificantCommits || options.ExitInsignificantCommits)
            {
                var exitCode = options.ExitInsignificantCommits ? 1 : 0;
                throw new VersionizeException(ErrorMessages.VersionUnaffected(version.ToNormalizedString()), exitCode);
            }
            else
            {
                nextVersion = nextVersion.IncrementPatchVersion();
            }
        }

        if (!string.IsNullOrWhiteSpace(options.ReleaseAs))
        {
            if (!SemanticVersion.TryParse(options.ReleaseAs, out nextVersion!))
            {
                throw new VersionizeException(ErrorMessages.CouldNotParseReleaseVersion(options.ReleaseAs), 1);
            }
        }

        if (version is not null && nextVersion < version)
        {
            throw new VersionizeException(ErrorMessages.SemanticVersionConflict(nextVersion.ToNormalizedString(), version.ToNormalizedString()), 1);
        }

        return nextVersion;
    }

    public sealed class Options
    {
        public bool IgnoreInsignificantCommits { get; init; }
        public bool ExitInsignificantCommits { get; init; }
        public string? Prerelease { get; init; }
        public string? ReleaseAs { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                ExitInsignificantCommits = versionizeOptions.ExitInsignificantCommits,
                IgnoreInsignificantCommits = versionizeOptions.IgnoreInsignificantCommits,
                Prerelease = versionizeOptions.Prerelease,
                ReleaseAs = versionizeOptions.ReleaseAs,
            };
        }
    }
}
