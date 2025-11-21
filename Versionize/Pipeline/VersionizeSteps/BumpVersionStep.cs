using NuGet.Versioning;
using Versionize.CommandLine;
using Versionize.Config;
using Versionize.Versioning;

namespace Versionize.Pipeline.VersionizeSteps;

public class BumpVersionStep : IBumpVersionStep
{
    private readonly VersionIncrementStrategy _incrementStrategy;

    public BumpVersionStep(VersionIncrementStrategy incrementStrategy)
    {
        _incrementStrategy = incrementStrategy;
    }

    public SemanticVersion Execute(IBumpVersionStep.Input input, IBumpVersionStep.Options options)
    {
        var originalVersion = input.OriginalVersion;
        var commits = input.ConventionalCommits;
        var insignificantCommitsAffectVersion = !(options.IgnoreInsignificantCommits || options.ExitInsignificantCommits);
        SemanticVersion nextVersion = originalVersion is null
            ? originalVersion ?? new SemanticVersion(1, 0, 0)
            : _incrementStrategy.NextVersion(commits, originalVersion, options.Prerelease, insignificantCommitsAffectVersion);

        if (nextVersion == originalVersion)
        {
            if (options.IgnoreInsignificantCommits || options.ExitInsignificantCommits)
            {
                var exitCode = options.ExitInsignificantCommits ? 1 : 0;
                throw new VersionizeException(ErrorMessages.VersionUnaffected(originalVersion.ToNormalizedString()), exitCode);
            }
            else
            {
                nextVersion = nextVersion.IncrementPatchVersion();
            }
        }

        if (!string.IsNullOrWhiteSpace(options.ReleaseAs))
        {
            if (!SemanticVersion.TryParse(options.ReleaseAs, out var version))
            {
                throw new VersionizeException(ErrorMessages.CouldNotParseReleaseVersion(options.ReleaseAs), 1);
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
