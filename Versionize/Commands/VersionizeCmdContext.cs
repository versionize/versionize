using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.CommandLine;
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
    IRepoStateValidator _repoStateValidator,
    IBumpFileProvider _bumpFileProvider) : IVersionizeCmdContextProvider
{
    public VersionizeCmdContext GetContext()
    {
        var options = _optionsProvider.GetOptions();

        CommandLineUI.Verbosity = options.Silent
            ? CommandLine.LogLevel.Silent
            : CommandLine.LogLevel.All;

        Repository repository = _repositoryProvider.GetRepository(options.WorkingDirectory);
        _repoStateValidator.Validate(repository, options);
        IBumpFile? bumpFile = _bumpFileProvider.GetBumpFile(options);

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
    IBumpFile? BumpFile)
{
    public SemanticVersion? GetCurrentVersion()
    {
        if (BumpFile is null || Options.SkipBumpFile)
        {
            return Repository.Tags
                .Select(Options.Project.ExtractTagVersion)
                .Where(x => x is not null)
                .OrderDescending()
                .FirstOrDefault();
        }

        return BumpFile?.Version;
    }
}
