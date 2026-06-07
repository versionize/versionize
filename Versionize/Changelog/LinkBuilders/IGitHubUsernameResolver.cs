namespace Versionize.Changelog.LinkBuilders;

public interface IGitHubUsernameResolver
{
    string? ResolveUsername(string commitSha);
}
