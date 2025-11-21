using LibGit2Sharp;
using NuGet.Versioning;
using Versionize.BumpFiles;
using Versionize.ConventionalCommits;

namespace Versionize.Pipeline.VersionizeSteps;

public sealed class EmptyResult
{
    public static readonly EmptyResult Default = new();
    private EmptyResult() { }
}

public record InitWorkingCopyResult
{
    public required Repository Repository { get; init; }
    // repo + working dir
}
public record GetBumpFileResult : InitWorkingCopyResult
{
    public required IBumpFile BumpFile { get; init; }
}
public record ReadVersionResult : GetBumpFileResult
{
    public required SemanticVersion Version { get; init; }
}
public record ParseCommitsSinceLastVersionResult : ReadVersionResult
{
    public required bool IsFirstRelease { get; init; }
    public required IReadOnlyList<ConventionalCommit> Commits { get; init; }
}
public record BumpVersionResult : ParseCommitsSinceLastVersionResult
{
    public required SemanticVersion BumpedVersion { get; init; }
}
public record UpdateChangelogResult : BumpVersionResult
{
    public required string ChangelogPath { get; init; }
}
public record CreateCommitResult : UpdateChangelogResult
{
    public required LibGit2Sharp.Commit Commit { get; init; }
}
public record CreateTagResult : CreateCommitResult
{
    public required LibGit2Sharp.Tag Tag { get; init; }
}
