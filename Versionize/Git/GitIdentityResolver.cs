using LibGit2Sharp;

namespace Versionize.Git;

public readonly record struct GitIdentity(string? UserName, string? UserEmail)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(UserEmail);
}

public interface IGitIdentityResolver
{
    GitIdentity Resolve(IRepository repository, string? gitUserName, string? gitUserEmail);
}

public sealed class GitIdentityResolver : IGitIdentityResolver
{
    public GitIdentity Resolve(IRepository repository, string? gitUserName, string? gitUserEmail)
    {
        var resolvedUserName = string.IsNullOrWhiteSpace(gitUserName)
            ? repository.Config.Get<string>("user.name")?.Value
            : gitUserName;

        var resolvedUserEmail = string.IsNullOrWhiteSpace(gitUserEmail)
            ? repository.Config.Get<string>("user.email")?.Value
            : gitUserEmail;

        return new GitIdentity(resolvedUserName, resolvedUserEmail);
    }
}
