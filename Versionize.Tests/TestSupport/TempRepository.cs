using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace Versionize.Tests.TestSupport
{
    public static class TempRepository
    {
        public static Repository Create(string path)
        {
            Repository.Init(path);

            // Initialize git repo
            var repo = new Repository(path);

            // Make sure we have a git author or versionize will fail to make a commit
            repo.Config.Set("user.name", "VersionizeTest");
            repo.Config.Set("user.email", "noreply@versionize.com");

            return repo;
        }
    }
}
