using Versionize.Config;
using Versionize.Git;

namespace Versionize.Tests.TestSupport;

internal static class GitIdentityResolverTestHelper
{
    internal static IGitIdentityResolver Create(string gitUserName = null, string gitUserEmail = null)
    {
        return new GitIdentityResolver(new TestVersionizeOptionsProvider(gitUserName, gitUserEmail));
    }

    private sealed class TestVersionizeOptionsProvider(string gitUserName, string gitUserEmail) : IVersionizeOptionsProvider
    {
        private readonly VersionizeOptions _options = new()
        {
            GitUserName = gitUserName,
            GitUserEmail = gitUserEmail,
            // Set required options with default values to avoid triggering unrelated validation errors in tests that use this helper.
            CommitParser = CommitParserOptions.Default,
            Project = ProjectOptions.DefaultOneProjectPerRepo,
            WorkingDirectory = string.Empty,
        };

        public VersionizeOptions GetOptions()
        {
            return _options;
        }
    }
}
