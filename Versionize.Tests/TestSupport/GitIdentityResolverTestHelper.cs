using Versionize.Config;
using Versionize.Git;

namespace Versionize.Tests.TestSupport;

internal static class GitIdentityResolverTestHelper
{
    internal static IGitIdentityResolver Create() => new GitIdentityResolver();
}
