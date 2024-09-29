﻿namespace Versionize;

public class ConventionalCommit
{
    public string Sha { get; set; }

    public string Scope { get; set; }

    public string Type { get; set; }

    public string Subject { get; set; }

    public List<ConventionalCommitNote> Notes { get; set; } = new List<ConventionalCommitNote>();

    public List<ConventionalCommitIssue> Issues { get; set; } = new List<ConventionalCommitIssue>();

    public bool IsFeature => Type == "feat";
    public bool IsFix => Type == "fix";
    public bool IsBreakingChange => Notes.Any(note => "BREAKING CHANGE".Equals(note.Title));
}

public class ConventionalCommitNote
{
    public string Title { get; set; }

    public string Text { get; set; }
}

public class ConventionalCommitIssue
{
    public string Token { get; set; }

    public string Id { get; set; }
}
