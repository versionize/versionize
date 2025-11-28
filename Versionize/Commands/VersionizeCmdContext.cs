using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.Config;
using Versionize.Git;

namespace Versionize.Commands;

internal interface IVersionizeCmdContextProvider
{
    VersionizeCmdContext GetContext();
}

internal sealed class VersionizeCmdContextProvider(
    IVersionizeOptionsProvider _optionsProvider,
    IRepositoryProvider _repositoryProvider,
    IBumpFileProvider _bumpFileProvider) : IVersionizeCmdContextProvider
{
    public VersionizeCmdContext GetContext()
    {
        var options = _optionsProvider.GetOptions();
        var repository = _repositoryProvider.GetRepositoryAndValidate(options);
        var bumpFile = _bumpFileProvider.GetBumpFile(options);

        return new VersionizeCmdContext(
            options,
            repository,
            bumpFile
        );
    }
}

internal sealed record VersionizeCmdContext(
    VersionizeOptions Options,
    Repository Repository,
    IBumpFile BumpFile)
{
    private static readonly SemanticVersion NullVersion = new(0, 0, 0);
    public SemanticVersion? GetCurrentVersion()
    {
        if (BumpFile.Version == NullVersion || Options.SkipBumpFile)
        {
            return Repository.Tags
                .Select(Options.Project.ExtractTagVersion)
                .Where(x => x is not null)
                .OrderByDescending(x => x)
                .FirstOrDefault();
        }

        return BumpFile.Version;
    }
}
