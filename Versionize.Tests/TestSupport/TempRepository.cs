using LibGit2Sharp;

namespace Versionize.Tests.TestSupport;

public static class TempRepository
{
    public static Repository Create(string path)
    {
        Repository.Init(path);
        var repo = new Repository(path);
        repo.Config.Set("user.name", "VersionizeTest");
        repo.Config.Set("user.email", "noreply@versionize.com");

        return repo;
    }
}
