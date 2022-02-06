using System.Text.Json.Serialization;

namespace Versionize
{
    // TODO: Consider creating a ChangelogConfigurationContract as a layer of abstraction.
    public record class ChangelogOptions
    {
        public const string Preamble = "# Change Log\n\nAll notable changes to this project will be documented in this file. See [versionize](https://github.com/saintedlama/versionize) for commit guidelines.\n";
        public static readonly ChangelogOptions Default = new ChangelogOptions
        {
            Header = Preamble,
            IncludeAllCommits = false,
            Sections = new ChangelogSection[]
            {
                new ChangelogSection { Type = "feat", Section = "Features", Hidden = false },
                new ChangelogSection { Type = "fix", Section = "Bug Fixes", Hidden = false },
            }
        };

        public string Header { get; set; }
        // Ignoring this serialization option for the moment, since ConfigurationContract
        // already has this option. It would be nice to remove that other option in favor
        // of this one, but it would be a breaking change.
        [JsonIgnore]
        public bool IncludeAllCommits { get; set; }
        public IEnumerable<ChangelogSection> Sections { get; set; }
    }
}
