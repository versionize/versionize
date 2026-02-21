using LibGit2Sharp;
using Versionize.CommandLine;
using Versionize.Config;

namespace Versionize.Git;

public interface IGitIdentityResolver
{
    bool IsConfigured(IRepository repository);
    Signature BuildSignature(IRepository repository, DateTimeOffset now);
    string BuildGitConfigArguments(IRepository repository);
}

public sealed class GitIdentityResolver(IVersionizeOptionsProvider configProvider) : IGitIdentityResolver
{
    private readonly IVersionizeOptionsProvider _configProvider = configProvider;

    public bool IsConfigured(IRepository repository)
    {
        var identity = Resolve(repository);
        return !string.IsNullOrWhiteSpace(identity.UserName) && !string.IsNullOrWhiteSpace(identity.UserEmail);
    }

    public Signature BuildSignature(IRepository repository, DateTimeOffset now)
    {
        var identity = Resolve(repository);
        if (string.IsNullOrWhiteSpace(identity.UserName) || string.IsNullOrWhiteSpace(identity.UserEmail))
        {
            throw new VersionizeException(ErrorMessages.GitConfigMissing(), 1);
        }

        return new Signature(identity.UserName, identity.UserEmail, now);
    }

    public string BuildGitConfigArguments(IRepository repository)
    {
        var identity = Resolve(repository);
        if (string.IsNullOrWhiteSpace(identity.UserName) || string.IsNullOrWhiteSpace(identity.UserEmail))
        {
            return string.Empty;
        }

        return $"-c user.name=\"{EscapeGitConfigValue(identity.UserName)}\" -c user.email=\"{EscapeGitConfigValue(identity.UserEmail)}\"";
    }

    private GitIdentity Resolve(IRepository repository)
    {
        var resolvedUserName = string.IsNullOrWhiteSpace(_configProvider.GetOptions().GitUserName)
            ? repository.Config.Get<string>("user.name")?.Value
            : _configProvider.GetOptions().GitUserName;

        var resolvedUserEmail = string.IsNullOrWhiteSpace(_configProvider.GetOptions().GitUserEmail)
            ? repository.Config.Get<string>("user.email")?.Value
            : _configProvider.GetOptions().GitUserEmail;

        return new GitIdentity(resolvedUserName, resolvedUserEmail);
    }

    private static string EscapeGitConfigValue(string value)
    {
        return value.Replace("\"", "\\\"");
    }

    private readonly record struct GitIdentity(string? UserName, string? UserEmail);
}
