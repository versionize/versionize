namespace Versionize.Changelog.LinkBuilders;

public interface IUsernameResolver
{
    string? ResolveUsername(string commitSha);
}
