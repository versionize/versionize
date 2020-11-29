using System;
using System.IO;
using Xunit;
using Versionize.Tests.TestSupport;
using Versionize.CommandLine;
using LibGit2Sharp;
using Shouldly;

namespace Versionize.Tests
{
    public class RepositoryExtensionsTests : IDisposable
    {
        private readonly TestSetup _testSetup;
        private readonly TestPlatformAbstractions _testPlatformAbstractions;

        public RepositoryExtensionsTests()
        {
            _testSetup = TestSetup.Create();

            _testPlatformAbstractions = new TestPlatformAbstractions();
            CommandLineUI.Platform = _testPlatformAbstractions;
        }

        [Fact]
        public void ShouldSelectLightweight()
        {
            TempCsProject.Create(Path.Join(_testSetup.WorkingDirectory, "project1"), "2.0.0");
            var commit = CommitAll(_testSetup.Repository);

            _testSetup.Repository.Tags.Add($"v2.0.0", commit);

            var versionTag = _testSetup.Repository.SelectVersionTag(new System.Version(2, 0, 0));

            versionTag.ToString().ShouldBe("refs/tags/v2.0.0");
        }

        [Fact]
        public void ShouldSelectAnnotatedTags()
        {
            TempCsProject.Create(Path.Join(_testSetup.WorkingDirectory, "project1"), "2.0.0");
            var commit = CommitAll(_testSetup.Repository);

            _testSetup.Repository.Tags.Add($"v2.0.0", commit, GetAuthorSignature(), "Some annotation message without a version included");

            var versionTag = _testSetup.Repository.SelectVersionTag(new System.Version(2, 0, 0));

            versionTag.ToString().ShouldBe("refs/tags/v2.0.0");
        }

        private static Commit CommitAll(IRepository repository, string message = "feat: Initial commit")
        {
            var author = GetAuthorSignature();
            Commands.Stage(repository, "*");
            return repository.Commit(message, author, author);
        }

        private static Signature GetAuthorSignature()
        {
            return new Signature("Gitty McGitface", "noreply@git.com", DateTime.Now);
        }

        public void Dispose()
        {
            _testSetup.Dispose();
        }
    }
}