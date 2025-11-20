using NuGet.Versioning;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.ConventionalCommits;
using Versionize.Versioning;

namespace Versionize.Pipeline.VersionizeSteps;

public class BumpVersionStep
{
    private readonly VersionIncrementStrategy _incrementStrategy;
    private readonly Options _options;

    public BumpVersionStep(VersionIncrementStrategy incrementStrategy, Options options)
    {
        _incrementStrategy = incrementStrategy;
        _options = options;
    }

    public SemanticVersion Execute(SemanticVersion? currentVersion, IReadOnlyList<ConventionalCommit> commits)
    {
        var insignificantCommitsAffectVersion = !(_options.IgnoreInsignificantCommits || _options.ExitInsignificantCommits);
        SemanticVersion nextVersion = currentVersion is null
            ? currentVersion ?? new SemanticVersion(1, 0, 0)
            : _incrementStrategy.NextVersion(commits, currentVersion, _options.Prerelease, insignificantCommitsAffectVersion);

        if (nextVersion == currentVersion)
        {
            if (_options.IgnoreInsignificantCommits || _options.ExitInsignificantCommits)
            {
                var exitCode = _options.ExitInsignificantCommits ? 1 : 0;
                throw new VersionizeException(ErrorMessages.VersionUnaffected(currentVersion.ToNormalizedString()), exitCode);
            }
            else
            {
                nextVersion = nextVersion.IncrementPatchVersion();
            }
        }

        if (!string.IsNullOrWhiteSpace(_options.ReleaseAs))
        {
            if (!SemanticVersion.TryParse(_options.ReleaseAs, out var version))
            {
                throw new VersionizeException(ErrorMessages.CouldNotParseReleaseVersion(_options.ReleaseAs), 1);
            }

            nextVersion = version;
        }

        return nextVersion;
    }

    public sealed class Options : IConvertibleFromVersionizeOptions<Options>
    {
        public bool IgnoreInsignificantCommits { get; init; }
        public bool ExitInsignificantCommits { get; init; }
        public string? Prerelease { get; init; }
        public string? ReleaseAs { get; init; }

        public static Options FromVersionizeOptions(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                IgnoreInsignificantCommits = versionizeOptions.IgnoreInsignificantCommits,
                ExitInsignificantCommits = versionizeOptions.ExitInsignificantCommits,
                Prerelease = versionizeOptions.Prerelease,
                ReleaseAs = versionizeOptions.ReleaseAs,
            };
        }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return FromVersionizeOptions(versionizeOptions);
        }
    }
}
