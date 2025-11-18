using Versionize.Config;
using Versionize.Git;
using Versionize.Lifecycle;
using Versionize.Pipeline.VersionizeSteps;

namespace Versionize.Pipeline;

public static class Pipeline
{
    public static Pipeline2<EmptyResult> Begin(VersionizeOptions options)
        => new(EmptyResult.Default, options);
}

public static class PipelineExtensions
{
    public static GetBumpFileResult DoSomething(this IPipelineStep<InitWorkingCopyResult, GetBumpFileStep.Options, GetBumpFileResult> @this, InitWorkingCopyResult input, VersionizeOptions options)
    {
        return @this.Execute(input, options);
    }

    public static Pipeline<InitWorkingCopyResult> InitWorkingCopy(this Pipeline<EmptyResult> p)
        => p.Then<InitWorkingCopyStep, InitWorkingCopyStep.Options, InitWorkingCopyResult>();

    public static Pipeline<GetBumpFileResult> GetBumpFile(this Pipeline<InitWorkingCopyResult> p)
        => p.Then<GetBumpFileStep, GetBumpFileStep.Options, GetBumpFileResult>();

    public static Pipeline<ReadVersionResult> ReadVersion(this Pipeline<GetBumpFileResult> p)
        => p.Then<ReadVersionStep, ReadVersionStep.Options, ReadVersionResult>();

    public static Pipeline<ParseCommitsSinceLastVersionResult> ParseCommits(this Pipeline<ReadVersionResult> p)
        => p.Then<ParseCommitsSinceLastVersionStep, ParseCommitsSinceLastVersionStep.Options, ParseCommitsSinceLastVersionResult>();

    public static Pipeline<BumpVersionResult> BumpVersion(this Pipeline<ParseCommitsSinceLastVersionResult> p)
        => p.Then<BumpVersionStep, BumpVersionStep.Options, BumpVersionResult>();

    public static Pipeline<UpdateChangelogResult> UpdateChangelog(this Pipeline<BumpVersionResult> p)
        => p.Then<UpdateChangelogStep, UpdateChangelogStep.Options, UpdateChangelogResult>();

    public static Pipeline<CreateCommitResult> CreateCommit(this Pipeline<UpdateChangelogResult> p)
        => p.Then<CreateCommitStep, CreateCommitStep.Options, CreateCommitResult>();

    public static Pipeline<CreateTagResult> CreateTag(this Pipeline<CreateCommitResult> p)
        => p.Then<CreateTagStep, CreateTagStep.Options, CreateTagResult>();

    // public static Pipeline<BumpFileData> Then<TStep>(this Pipeline<InitData> pipeline)
    //     where TStep : IPipelineStep<InitData, BumpFileData, BumpFileProvider.Options>
    //     => pipeline.Then<TStep, BumpFileData, BumpFileProvider.Options>();
}
