using LibGit2Sharp;
using Versionize.Config;
using Versionize.Git;

namespace Versionize.Commands;

internal interface IInspectCmdContextProvider
{
    InspectCmdContext GetContext();
}

internal sealed class InspectCmdContextProvider(
    IVersionizeOptionsProvider _optionsProvider,
    IRepositoryProvider _repositoryProvider) : IInspectCmdContextProvider
{
    public InspectCmdContext GetContext()
    {
        VersionizeOptions options = _optionsProvider.GetOptions();
        IRepository repository = _repositoryProvider.GetRepository(options);

        var inspectOptions = new InspectCmdOptions
        {
            WorkingDirectory = options.WorkingDirectory,
            ProjectOptions = options.Project,
            SkipBumpFile = options.SkipBumpFile
        };

        return new InspectCmdContext(
            inspectOptions,
            repository
        );
    }
}

internal sealed record InspectCmdContext(
    InspectCmdOptions Options,
    IRepository Repository);

internal sealed record InspectCmdOptions
{
    public required string WorkingDirectory { get; init; }
    public required ProjectOptions ProjectOptions { get; init; }
    public required bool SkipBumpFile { get; init; }
}
