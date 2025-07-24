using NuGet.Versioning;
using Versionize.Config;
using Versionize.ConventionalCommits;
using Versionize.Versioning;
using static Versionize.CommandLine.CommandLineUI;

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

        var allowInsignificantCommits = !(options.IgnoreInsignificantCommits || options.ExitInsignificantCommits);
        SemanticVersion nextVersion = isInitialRelease || version is null
            ? version ?? new SemanticVersion(1, 0, 0)
            : versionIncrement.NextVersion(version, options.Prerelease, allowInsignificantCommits);

        if (!isInitialRelease && nextVersion == version)
        {
            if (options.IgnoreInsignificantCommits || options.ExitInsignificantCommits)
            {
                var exitCode = options.ExitInsignificantCommits ? 1 : 0;
                Exit($"Version was not affected by commits since last release ({version})", exitCode);
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
                Exit($"Could not parse the specified release version {options.ReleaseAs} as valid version", 1);
            }
        }

        if (version is not null && nextVersion! < version)
        {
            Exit($"Semantic versioning conflict: the next version {nextVersion} would be lower than the current version {version}. This can be caused by using a wrong pre-release label or release as version", 1);
        }

        return nextVersion!;
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
