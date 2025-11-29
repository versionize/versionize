using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.Config;
using Versionize.Git;

namespace Versionize.Commands;

internal interface IInspectCmdContextProvider
{
    InspectCmdContext GetContext();
}

internal sealed class InspectCmdContextProvider(
    IVersionizeOptionsProvider _optionsProvider,
    IRepositoryProvider _repositoryProvider,
    IBumpFileProvider _bumpFileProvider) : IInspectCmdContextProvider
{
    public InspectCmdContext GetContext()
    {
        VersionizeOptions options = _optionsProvider.GetOptions();
        IRepository repository = _repositoryProvider.GetRepository(options.WorkingDirectory);
        IBumpFile bumpFile = _bumpFileProvider.GetBumpFile(options);

        var inspectOptions = new InspectCmdOptions
        {
            WorkingDirectory = options.WorkingDirectory,
            ProjectOptions = options.Project,
            SkipBumpFile = options.SkipBumpFile
        };

        return new InspectCmdContext(
            inspectOptions,
            repository,
            bumpFile
        );
    }
}

internal sealed record InspectCmdContext(
    InspectCmdOptions Options,
    IRepository Repository,
    IBumpFile BumpFile)
{
    private static readonly SemanticVersion NullVersion = new(0, 0, 0);
    public SemanticVersion? GetCurrentVersion()
    {
        if (BumpFile.Version == NullVersion || Options.SkipBumpFile)
        {
            return Repository.Tags
                .Select(Options.ProjectOptions.ExtractTagVersion)
                .Where(x => x is not null)
                .OrderByDescending(x => x)
                .FirstOrDefault();
        }

        return BumpFile.Version;
    }
}

internal sealed record InspectCmdOptions
{
    public required string WorkingDirectory { get; init; }
    public required ProjectOptions ProjectOptions { get; init; }
    public required bool SkipBumpFile { get; init; }
}
