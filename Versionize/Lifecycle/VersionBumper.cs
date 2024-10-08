using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.ConventionalCommits;

namespace Versionize;

public interface IVersionBumper
{
    (SemanticVersion, IReadOnlyList<ConventionalCommit>) Bump(
        SemanticVersion version,
        Projects projectGroup);
}

//public sealed class VersionBumper : IVersionBumper
//{
//    private readonly IVersionCalculator _versionCalculator;
//    private readonly IConventionalCommitProvider _conventionalCommitProvider;
//    private readonly IBumpFileUpdater _bumpFileUpdater;

//    public VersionBumper(
//        IVersionCalculator versionCalculator,
//        IConventionalCommitProvider conventionalCommitProvider,
//        IBumpFileUpdater bumpFileUpdater)
//    {
//        _versionCalculator = versionCalculator;
//        _conventionalCommitProvider = conventionalCommitProvider;
//        _bumpFileUpdater = bumpFileUpdater;
//    }

//    public (SemanticVersion, IReadOnlyList<ConventionalCommit>) Bump(
//        SemanticVersion version,
//        Projects projectGroup)
//    {
//        var (isInitialRelease, conventionalCommits) = _conventionalCommitProvider.GetAll(version);
//        var nextVersion = _versionCalculator.Bump(version, isInitialRelease, conventionalCommits);
//        _bumpFileUpdater.Update(nextVersion, projectGroup);
//        return (nextVersion, conventionalCommits);
//    }
//}
