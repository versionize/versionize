using NuGet.Versioning;
using Versionize.Config;
using Versionize.ConventionalCommits;
using Versionize.Versioning;
using Versionize.CommandLine;

using Input = Versionize.Lifecycle.IVersionBumper.Input;
using Options = Versionize.Lifecycle.IVersionBumper.Options;

namespace Versionize.Lifecycle;

public sealed class VersionBumper : IVersionBumper
{
    // private readonly VersionIncrementStrategy _incrementStrategy;

    // public VersionCalculator(VersionIncrementStrategy incrementStrategy)
    // {
    //     _incrementStrategy = incrementStrategy;
    // }

    public SemanticVersion Bump(Input input, Options options)
    {
        var version = input.OriginalVersion;
        var conventionalCommits = input.ConventionalCommits;

        var isInitialRelease = version is null;
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
}

public interface IVersionBumper
{
    SemanticVersion Bump(Input input, Options options);

    sealed class Input
    {
        public required SemanticVersion? OriginalVersion { get; init; }
        public required IReadOnlyList<ConventionalCommit> ConventionalCommits { get; init; }
    }

    sealed class Options
    {
        public bool IgnoreInsignificantCommits { get; init; }
        public bool ExitInsignificantCommits { get; init; }
        public string? Prerelease { get; init; }
        public string? ReleaseAs { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                IgnoreInsignificantCommits = versionizeOptions.IgnoreInsignificantCommits,
                ExitInsignificantCommits = versionizeOptions.ExitInsignificantCommits,
                Prerelease = versionizeOptions.Prerelease,
                ReleaseAs = versionizeOptions.ReleaseAs,
            };
        }
    }
}
