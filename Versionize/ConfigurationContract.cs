using System;

namespace Versionize
{
    public class ConfigurationContract
    {
        public bool? Silent { get; set; }
        public bool? DryRun { get; set; }
        public bool? SkipDirty { get; set; }
        public bool? SkipCommit { get; set; }
        public string ReleaseAs { get; set; }
        public bool? IgnoreInsignificantCommits { get; set; }
        public bool? ChangelogAll { get; set; }
        public string CommitSuffix { get; set; }
        public string Prerelease { get; set; }
        public ChangelogOptions Changelog { get; set; }
    }
}
