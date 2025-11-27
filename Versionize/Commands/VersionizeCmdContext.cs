using LibGit2Sharp;
using Versionize.Config;
using Versionize.Git;

namespace Versionize.Commands;

internal interface IVersionizeCmdContextProvider
{
    VersionizeCmdContext GetContext();
}

internal sealed class VersionizeCmdContextProvider(
    IVersionizeOptionsProvider _optionsProvider,
    IRepositoryProvider _repositoryProvider) : IVersionizeCmdContextProvider
{
    public VersionizeCmdContext GetContext()
    {
        var options = _optionsProvider.GetOptions();
        var repository = _repositoryProvider.GetRepositoryAndValidate(options);

        return new VersionizeCmdContext(
            options,
            repository
        );
    }
}

internal sealed record VersionizeCmdContext(
    VersionizeOptions Options,
    Repository Repository);
