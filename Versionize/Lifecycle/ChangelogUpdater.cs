using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.Changelog;
using Versionize.Config;
using Versionize.ConventionalCommits;
using static Versionize.CommandLine.CommandLineUI;
using Versionize.CommandLine;
using Versionize.Changelog.LinkBuilders;
using System.Text;
using System.Runtime.CompilerServices;

using Input = Versionize.Lifecycle.IChangelogUpdater.Input;
using Options = Versionize.Lifecycle.IChangelogUpdater.Options;

namespace Versionize.Lifecycle;

public sealed class ChangelogWriter
{
    private readonly IMarkdown _markdown;

    public void Append(string text)
    {
        // Implementation to write a line in markdown
    }
    public void Append(
        [InterpolatedStringHandlerArgument("")]
        ref StringBuilder.AppendInterpolatedStringHandler _)
    {
        // Work is done by the handler.
    }

    public void AppendChangeList(
        IEnumerable<ConventionalCommit> commits,
        ChangelogOptions changelogOptions)
    {
        // Implementation to generate changelog content from commits
    }

    public static implicit operator StringBuilder(ChangelogWriter writer)
    {
        return writer._markdown.StringBuilder;
    }

    private void GenerateCommitList(
        IChangelogLinkBuilder linkBuilder,
        IEnumerable<ConventionalCommit> commits,
        ChangelogOptions changelogOptions)
    {
        var visibleChangelogSections = changelogOptions.Sections is null
            ? []
            : changelogOptions.Sections.Where(x => !x.Hidden);

        foreach (var changelogSection in visibleChangelogSections)
        {
            var matchingCommits = commits.Where(commit => string.Equals(commit.Type, changelogSection.Type, StringComparison.OrdinalIgnoreCase));
            BuildBlock(changelogSection.Section, linkBuilder, matchingCommits);
        }

        BuildBlock("Breaking Changes", linkBuilder, commits.Where(commit => commit.IsBreakingChange));

        if (changelogOptions.IncludeAllCommits.GetValueOrDefault())
        {
            BuildBlock(
                changelogOptions.OtherSection ?? "Other",
                linkBuilder,
                commits.Where(commit => !visibleChangelogSections.Any(x => string.Equals(x.Type, commit.Type, StringComparison.OrdinalIgnoreCase)) && !commit.IsBreakingChange));
        }
    }

    private void BuildBlock(string? header, IChangelogLinkBuilder linkBuilder, IEnumerable<ConventionalCommit> commits)
    {
        if (string.IsNullOrEmpty(header) || !commits.Any())
        {
            return;
        }

        _markdown.Heading(header, 3);

        commits = commits
            .OrderBy(c => c.Scope)
            .ThenBy(c => c.Subject);

        foreach (var commit in commits)
        {
            BuildCommit(commit, linkBuilder);
            _markdown.StringBuilder.AppendLine();
        }
    }

    private void BuildCommit(ConventionalCommit commit, IChangelogLinkBuilder linkBuilder)
    {
        _markdown.StringBuilder.Append("* ");

        if (!string.IsNullOrEmpty(commit.Scope))
        {
            _markdown.Bold($"{commit.Scope}:");
            _markdown.StringBuilder.Append(' ');
        }

        var subject = commit.Subject;
        foreach (var issue in commit.Issues)
        {
            if (string.IsNullOrEmpty(subject))
            {
                continue;
            }
            if (string.IsNullOrEmpty(issue.Id))
            {
                continue;
            }
            if (string.IsNullOrEmpty(issue.Token))
            {
                continue;
            }

            var issueLink = linkBuilder.BuildIssueLink(issue.Id);
            if (!string.IsNullOrEmpty(issueLink))
            {
                //subject = subject.Replace(issue.Token, $"[{issue.Token}]({issueLink})");
                var formattedLink = _markdown.GetLink(issue.Token, issueLink);
                subject = subject.Replace(issue.Token, formattedLink);
            }
        }

        _markdown.StringBuilder.Append(subject).Append(' ');

        var commitLink = linkBuilder.BuildCommitLink(commit);

        if (!string.IsNullOrEmpty(commitLink))
        {
            ReadOnlySpan<char> shaSpan = string.IsNullOrEmpty(commit.Sha)
                ? ""
                : commit.Sha.AsSpan(0, Math.Min(7, commit.Sha.Length));
            //_markdown.Link($" ([{shaSpan}]({commitLink}))");
            _markdown.Link(shaSpan, commitLink);
        }
    }
}

public interface IMarkdown
{
    StringBuilder StringBuilder { get; }
    void Heading(string text, int level);
    void Bold(string text);
    void Link(string display, string link);
    void Link(ReadOnlySpan<char> display, string link);
    string GetLink(string display, string link);
    void List(string[] items);
    // void ListItem(string text);
    // void ListItem(
    //     [InterpolatedStringHandlerArgument("")]
    //     ref StringBuilder.AppendInterpolatedStringHandler text);
}

public sealed class GitHubMarkdown : IMarkdown
{
    private readonly StringBuilder _sb = new();

    public StringBuilder StringBuilder => _sb;

    public void Heading(string text, int level)
    {
        //`${'#'.repeat(level)} ${text}\n\n`
        _sb.Append('#', level);
        _sb.Append(' ');
        _sb.AppendLine(text);
        _sb.AppendLine();
    }
    public void Bold(string text)
    {
        //`**${text}**`
        _sb.Append('*', 2);
        _sb.Append(text);
        _sb.Append('*', 2);
    }
    public void Link(string display, string link)
    {
        //`[${display}](${link})`
        _sb.Append($"[{display}]({link})");
    }
    public void Link(
        [InterpolatedStringHandlerArgument("")]
        ref StringBuilder.AppendInterpolatedStringHandler display, string link)
    {
        //`[${display}](${link})`
        _sb.Append($"[{display}]({link})");
    }
    public void List(string[] items)
    {
        //`* ${list.join('\n* ')}\n\n`
        _sb.Append("* ");
        _sb.AppendJoin("\n* ", items);
        // foreach (var item in items)
        // {
        //     _sb.Append("* ");
        //     _sb.AppendLine(item);
        // }
        _sb.AppendLine();
    }

    // public void ListItem(string text)
    // {
    //     throw new NotImplementedException();
    // }

    // public void ListItem([InterpolatedStringHandlerArgument("")] ref StringBuilder.AppendInterpolatedStringHandler text)
    // {
    //     throw new NotImplementedException();
    // }

    public void Link(ReadOnlySpan<char> display, string link)
    {
        _sb.Append($"[{display}]({link})");
        // _sb.Append('[');
        // _sb.Append(display);
        // _sb.Append($"]({link})");
    }

    public string GetLink(string display, string link)
    {
        return $"[{display}]({link})";
    }
}

public sealed class ChangelogFile
{
    private readonly ChangelogWriter _changelogWriter;

    public void Update(Input input, Options options)
    {
        var repo = input.Repository;
        var nextVersion = input.NewVersion;
        var previousVersion = input.OriginalVersion ?? nextVersion;
        var conventionalCommits = input.ConventionalCommits;
        var versionTime = DateTimeOffset.Now;
        var projectOptions = options.Project;
        var changelogOptions = options.Project.Changelog;
        //IChangelogLinkBuilder linkBuilder
        // placeholder impl:
        _changelogWriter.Append($"## {nextVersion} - {versionTime:yyyy-MM-dd}");
        _changelogWriter.AppendChangeList(conventionalCommits, changelogOptions);

        var currentTag = projectOptions.GetTagName(nextVersion);
        var previousTag = projectOptions.GetTagName(previousVersion);
        var compareUrl = linkBuilder.BuildVersionTagLink(currentTag, previousTag);
        var versionTagLink = string.IsNullOrWhiteSpace(compareUrl)
            ? nextVersion.ToString()
            : $"[{nextVersion}]({compareUrl})";

        _changelogWriter.Append($"<a name=\"{nextVersion}\"></a>");
        _changelogWriter.Append("\n");
        _changelogWriter.Append($"## {versionTagLink} ({versionTime:yyyy-MM-dd})");
        _changelogWriter.Append("\n");
        _changelogWriter.Append("\n");

        // TODO: Write to file or dry run output
    }
}

public sealed class ChangelogUpdater : IChangelogUpdater
{
    public ChangelogBuilder? Update(Input input, Options options)
    {
        var repo = input.Repository;
        var nextVersion = input.NewVersion;
        var previousVersion = input.OriginalVersion;
        var conventionalCommits = input.ConventionalCommits;

        if (options.SkipChangelog)
        {
            return null;
        }

        // TODO: Consider using TimeProvider?
        var versionTime = DateTimeOffset.Now;

        // TODO: Consider constructing this path when creating options.
        var changelogPath = Path.GetFullPath(Path.Combine(options.WorkingDirectory, options.Project.Changelog.Path ?? ""));
        var changelog = ChangelogBuilder.CreateForPath(changelogPath);
        var changelogLinkBuilder = LinkBuilderFactory.CreateFor(repo, options.Project.Changelog.LinkTemplates);
        previousVersion ??= nextVersion;

        if (options.DryRun)
        {
            string markdown = ChangelogBuilder.GenerateMarkdown(
                nextVersion,
                previousVersion,
                versionTime,
                changelogLinkBuilder,
                conventionalCommits,
                options.Project);
            DryRun(markdown.TrimEnd('\n'));
        }
        else
        {
            changelog.Write(
                nextVersion,
                previousVersion,
                versionTime,
                changelogLinkBuilder,
                conventionalCommits,
                options.Project);
        }
        Step(InfoMessages.UpdatedChangelog());

        return changelog;
    }
}

public interface IChangelogUpdater
{
    ChangelogBuilder? Update(Input input, Options options);

    sealed class Input
    {
        public required Repository Repository { get; init; }
        public required SemanticVersion NewVersion { get; init; }
        public required SemanticVersion? OriginalVersion { get; init; }
        public required IReadOnlyList<ConventionalCommit> ConventionalCommits { get; init; }
    }

    sealed class Options
    {
        public bool SkipChangelog { get; init; }
        public bool DryRun { get; init; }
        public required ProjectOptions Project { get; init; }
        public required string WorkingDirectory { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                DryRun = versionizeOptions.DryRun,
                SkipChangelog = versionizeOptions.SkipChangelog,
                Project = versionizeOptions.Project,
                WorkingDirectory = versionizeOptions.WorkingDirectory,
            };
        }
    }
}
