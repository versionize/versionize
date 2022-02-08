using System.Linq;
using NuGet.Versioning;

namespace Versionize.Versioning
{
    public static class SemanticVersionExtensions
    {
        public static SemanticVersion AsPreRelease(this SemanticVersion version, string preReleaseLabel, int preReleaseNumber)
        {
            return new SemanticVersion(version.Major, version.Minor, version.Patch, new[] { preReleaseLabel, preReleaseNumber.ToString() }, null);
        }

        public static SemanticVersion AsRelease(this SemanticVersion version)
        {
            return new SemanticVersion(version.Major, version.Minor, version.Patch);
        }

        public static SemanticVersion IncrementPreRelease(this SemanticVersion version, string newPreReleaseLabel)
        {
            // TODO: A bit whacky
            var releaseLabels = version.ReleaseLabels.ToArray();

            var preReleaseLabel = releaseLabels[0];
            var preReleaseNumber = int.Parse(releaseLabels[1]);

            var newReleaseLabels = new string[] { newPreReleaseLabel, (preReleaseLabel.Equals(newPreReleaseLabel)?(preReleaseNumber + 1):0).ToString() };
            return new SemanticVersion(version.Major, version.Minor, version.Patch, newReleaseLabels, null);
        }

        public static SemanticVersion IncrementPatchVersion(this SemanticVersion version)
        {
            if (version.IsPrerelease)
            {
                // TODO: A bit whacky
                var preReleaseLabel = version.ReleaseLabels.First();

                return version.IncrementPreRelease(preReleaseLabel);
            }
            else
            {
                return new SemanticVersion(version.Major, version.Minor, version.Patch + 1);
            }
        }
    }
}
