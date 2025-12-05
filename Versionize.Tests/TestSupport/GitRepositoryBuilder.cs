using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using NSubstitute;
using NuGet.Versioning;

#nullable enable

namespace Versionize.Tests.TestSupport;

public sealed class GitRepositoryBuilder
{
    private readonly IRepository repository;
    private readonly TagCollection tagCollection;
    private readonly Configuration configuration;
    private readonly IQueryableCommitLog commitLog;
    private readonly RepositoryStatus repositoryStatus;

    private readonly List<Tag> tags = [];
    private readonly List<LogEntry> logEntries = [];
    private readonly List<StatusEntry> statusEntries = [];

    private string defaultCommitPath = "/";

    private GitRepositoryBuilder()
    {
        repository = Substitute.For<IRepository>();

        tagCollection = Substitute.For<TagCollection>();
        tagCollection.GetEnumerator().Returns(ci => tags.GetEnumerator());
        repository.Tags.Returns(tagCollection);
        // Add(string name, GitObject target, Signature tagger, string message)
        tagCollection.Add(Arg.Any<string>(), Arg.Any<GitObject>(), Arg.Any<Signature>(), Arg.Any<string>())
            .Returns(ci =>
            {
                var name = (string)ci[0];
                var target = (GitObject)ci[1];

                var tag = Substitute.For<Tag>();
                tag.FriendlyName.Returns(name);
                tag.Target.Returns(target);
                tags.Add(tag);
                return tag;
            });

        configuration = Substitute.For<Configuration>();
        repository.Config.Returns(configuration);
        WithUser("Test User", "testuser@example.com");
        configuration.BuildSignature(Arg.Any<DateTimeOffset>())
            .Returns(ci => new Signature(
                configuration.Get<string>("user.name").Value,
                configuration.Get<string>("user.email").Value,
                (DateTimeOffset)ci[0]));

        commitLog = Substitute.For<IQueryableCommitLog>();
        commitLog.GetEnumerator().Returns(ci => logEntries.Select(e => e.Commit).GetEnumerator());
        commitLog.QueryBy(Arg.Any<string>(), Arg.Any<CommitFilter>())
            .Returns(ci => logEntries.Where(e => e.Path == (string)ci[0]));
        repository.Commits.Returns(commitLog);
        // Commit(string message, Signature author, Signature committer, CommitOptions options)
        repository.Commit(Arg.Any<string>(), Arg.Any<Signature>(), Arg.Any<Signature>(), Arg.Any<CommitOptions>())
            .Returns(ci =>
            {
                var message = (string)ci[0];
                var author = (Signature)ci[1];
                var committer = (Signature)ci[2];
                var options = (CommitOptions)ci[3];

                var commit = Substitute.For<Commit>();
                commit.Message.Returns(message);
                commit.MessageShort.Returns(message);
                commit.Author.Returns(author);
                commit.Committer.Returns(committer);
                commit.Id.Returns(NewObjectId());

                var logEntry = new LogEntry();
                typeof(LogEntry).GetProperty("Commit")!.SetValue(logEntry, commit);
                typeof(LogEntry).GetProperty("Path")!.SetValue(logEntry, defaultCommitPath);
                logEntries.Add(logEntry);

                return commit;
            });

        repository.Head.Returns(Substitute.For<Branch>());
        repository.Head.Tip.Returns(ci => logEntries.LastOrDefault()?.Commit);

        repositoryStatus = Substitute.For<RepositoryStatus>();
        repositoryStatus.GetEnumerator().Returns(ci => statusEntries.GetEnumerator());
        repositoryStatus.IsDirty.Returns(false);
        repository.RetrieveStatus(Arg.Any<StatusOptions>()).Returns(repositoryStatus);
    }

    private static ObjectId NewObjectId()
    {
        var hex = Guid.NewGuid().ToString().Replace("-", "");
        hex = (hex + new string('0', 40))[..40];
        return new ObjectId(hex);
    }

    public static GitRepositoryBuilder Create()
    {
        return new GitRepositoryBuilder();
    }

    public GitRepositoryBuilder WithUser(string name, string email)
    {
        var nameEntry = Substitute.For<ConfigurationEntry<string>>();
        nameEntry.Key.Returns("user.name");
        nameEntry.Value.Returns(name);
        configuration.Get<string>("user.name").Returns(nameEntry);

        var emailEntry = Substitute.For<ConfigurationEntry<string>>();
        emailEntry.Key.Returns("user.email");
        emailEntry.Value.Returns(email);
        configuration.Get<string>("user.email").Returns(emailEntry);
        return this;
    }

    public GitRepositoryBuilder WithDefaultCommitPath(string path)
    {
        defaultCommitPath = path;
        return this;
    }

    public GitRepositoryBuilder WithCommit(string message, string? path = null)
    {
        var commit = Substitute.For<Commit>();
        commit.MessageShort.Returns(message);
        commit.Id.Returns(NewObjectId());

        var logEntry = new LogEntry();
        typeof(LogEntry).GetProperty("Commit")!.SetValue(logEntry, commit);
        typeof(LogEntry).GetProperty("Path")!.SetValue(logEntry, path ?? defaultCommitPath);
        logEntries.Add(logEntry);
        return this;
    }

    public GitRepositoryBuilder WithVersionTag(SemanticVersion version, Commit? commit = null)
    {
        commit ??= Substitute.For<Commit>();
        commit.MessageShort.Returns($"feat: version {version}");
        commit.Id.Returns(NewObjectId());

        var target = Substitute.For<GitObject>();
        target.Id.Returns(commit.Id);

        var tag = Substitute.For<Tag>();
        tag.FriendlyName.Returns($"v{version}");
        tag.Target.Returns(target);
        tags.Add(tag);
        return this;
    }

    public GitRepositoryBuilder WithTag(string friendlyName)
    {
        var target = Substitute.For<GitObject>();
        target.Id.Returns(NewObjectId());

        var tag = Substitute.For<Tag>();
        tag.FriendlyName.Returns(friendlyName);
        tag.Target.Returns(target);
        tags.Add(tag);
        return this;
    }

    public GitRepositoryBuilder WithDirty(params string[] filePaths)
    {
        repositoryStatus.IsDirty.Returns(true);
        foreach (var file in filePaths)
        {
            var statusEntry = Substitute.For<StatusEntry>();
            statusEntry.FilePath.Returns(file);
            statusEntries.Add(statusEntry);
        }
        return this;
    }

    public IRepository Build()
    {
        return repository;
    }

    // Intentionally no implicit conversion to IRepository (interfaces not allowed)
}