using LibGit2Sharp;
using Versionize.CommandLine;

namespace Versionize.Git;

internal interface IGitIdentityResolver
{
    bool IsConfigured(IRepository repository, string? gitUserName, string? gitUserEmail);
    Signature BuildSignature(IRepository repository, string? gitUserName, string? gitUserEmail, DateTimeOffset now);
    string BuildGitConfigArguments(IRepository repository, string? gitUserName, string? gitUserEmail);
}

internal sealed class GitIdentityResolver : IGitIdentityResolver
{
    public bool IsConfigured(IRepository repository, string? gitUserName, string? gitUserEmail)
    {
        var identity = Resolve(repository, gitUserName, gitUserEmail);
        return !string.IsNullOrWhiteSpace(identity.UserName) && !string.IsNullOrWhiteSpace(identity.UserEmail);
    }

    public Signature BuildSignature(IRepository repository, string? gitUserName, string? gitUserEmail, DateTimeOffset now)
    {
        var identity = Resolve(repository, gitUserName, gitUserEmail);
        if (string.IsNullOrWhiteSpace(identity.UserName) || string.IsNullOrWhiteSpace(identity.UserEmail))
        {
            throw new VersionizeException(ErrorMessages.GitConfigMissing(), 1);
        }

        return new Signature(identity.UserName, identity.UserEmail, now);
    }

    public string BuildGitConfigArguments(IRepository repository, string? gitUserName, string? gitUserEmail)
    {
        var identity = Resolve(repository, gitUserName, gitUserEmail);
        if (string.IsNullOrWhiteSpace(identity.UserName) || string.IsNullOrWhiteSpace(identity.UserEmail))
        {
            return string.Empty;
        }

        return $"-c user.name=\"{EscapeGitConfigValue(identity.UserName)}\" -c user.email=\"{EscapeGitConfigValue(identity.UserEmail)}\"";
    }

    private static GitIdentity Resolve(IRepository repository, string? gitUserName, string? gitUserEmail)
    {
        var resolvedUserName = string.IsNullOrWhiteSpace(gitUserName)
            ? repository.Config.Get<string>("user.name")?.Value
            : gitUserName;

        var resolvedUserEmail = string.IsNullOrWhiteSpace(gitUserEmail)
            ? repository.Config.Get<string>("user.email")?.Value
            : gitUserEmail;

        return new GitIdentity(resolvedUserName, resolvedUserEmail);
    }

    private static string EscapeGitConfigValue(string value)
    {
        return value.Replace("\"", "\\\"");
    }

    private readonly record struct GitIdentity(string? UserName, string? UserEmail);
}