using LibGit2Sharp;
using Versionize.Config;

namespace Versionize.Git;

public readonly record struct GitIdentity(string? UserName, string? UserEmail)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(UserEmail);
}

public interface IGitIdentityResolver
{
    GitIdentity Resolve(IRepository repository);
}

public sealed class GitIdentityResolver(IVersionizeOptionsProvider configProvider) : IGitIdentityResolver
{
    private readonly IVersionizeOptionsProvider _configProvider = configProvider;

    public GitIdentity Resolve(IRepository repository)
    {
        var options = _configProvider.GetOptions();
        var resolvedUserName = string.IsNullOrWhiteSpace(options.GitUserName)
            ? repository.Config.Get<string>("user.name")?.Value
            : options.GitUserName;

        var resolvedUserEmail = string.IsNullOrWhiteSpace(options.GitUserEmail)
            ? repository.Config.Get<string>("user.email")?.Value
            : options.GitUserEmail;

        return new GitIdentity(resolvedUserName, resolvedUserEmail);
    }
}
