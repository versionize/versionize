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

// public sealed class ChangelogWriter
// {
//     private readonly IMarkdown _markdown;

//     public void Append(string text)
//     {
//         // Implementation to write a line in markdown
//     }
//     public void Append(
//         [InterpolatedStringHandlerArgument("")]
//         ref StringBuilder.AppendInterpolatedStringHandler _)
//     {
//         // Work is done by the handler.
//     }

//     public void AppendChangeList(
//         IEnumerable<ConventionalCommit> commits,
//         ChangelogOptions changelogOptions)
//     {
//         // Implementation to generate changelog content from commits
//     }

//     public static implicit operator StringBuilder(ChangelogWriter writer)
//     {
//         return writer._markdown.StringBuilder;
//     }
// }
// public interface IMarkdown
// {
//     StringBuilder StringBuilder { get; }
//     void Heading(string text, int level);
//     void Bold(string text);
//     void Link(string display, string link);
//     void List(string[] items);
// }
// public sealed class GitHubMarkdown : IMarkdown
// {
//     private readonly StringBuilder _sb = new();

//     public StringBuilder StringBuilder => _sb;

//     public void Heading(string text, int level)
//     {
//         //`${'#'.repeat(level)} ${text}\n\n`
//         _sb.Append('#', level);
//         _sb.Append(' ');
//         _sb.AppendLine(text);
//         _sb.AppendLine();
//     }
//     public void Bold(string text)
//     {
//         //`**${text}**`
//         _sb.Append('*', 2);
//         _sb.Append(text);
//         _sb.Append('*', 2);
//         _sb.AppendLine($"{text}");
//     }
//     public void Link(string display, string link)
//     {
//         //`[${display}](${link})`
//         _sb.Append($"[{display}]({link})");
//     }
//     public void Link(
//         [InterpolatedStringHandlerArgument("")]
//         ref StringBuilder.AppendInterpolatedStringHandler display, string link)
//     {
//         //`[${display}](${link})`
//         _sb.Append($"[{display}]({link})");
//     }
//     public void List(string[] items)
//     {
//         //`* ${list.join('\n* ')}\n\n`
//         _sb.Append("* ");
//         _sb.AppendJoin("\n* ", items);
//         // foreach (var item in items)
//         // {
//         //     _sb.Append("* ");
//         //     _sb.AppendLine(item);
//         // }
//         _sb.AppendLine();
//     }
// }
// public sealed class ChangelogFile
// {
//     private readonly ChangelogWriter _changelogWriter;
//     public void Update(
//         SemanticVersion nextVersion,
//         SemanticVersion previousVersion,
//         DateTimeOffset versionTime,
//         IChangelogLinkBuilder linkBuilder,
//         IReadOnlyList<ConventionalCommit> conventionalCommits,
//         ProjectOptions projectOptions)
//     {
//         // placeholder impl:
//         _changelogWriter.Append($"## {nextVersion} - {versionTime:yyyy-MM-dd}");
//         _changelogWriter.AppendChangeList(conventionalCommits, projectOptions.Changelog);
//         // TODO: Write to file or dry run output
//     }
// }

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
