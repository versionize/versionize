using NuGet.Versioning;

namespace Versionize.Versioning;

public static class SemanticVersionExtensions
{
    public static SemanticVersion AsPrerelease(this SemanticVersion version, string prereleaseLabel, int prereleaseNumber)
    {
        return new SemanticVersion(version.Major, version.Minor, version.Patch, new[] { prereleaseLabel, prereleaseNumber.ToString() }, null);
    }

    public static SemanticVersion AsRelease(this SemanticVersion version)
    {
        return new SemanticVersion(version.Major, version.Minor, version.Patch);
    }

    public static SemanticVersion IncrementPrerelease(this SemanticVersion version, string newPrereleaseLabel)
    {
        var prereleaseIdentifier = PrereleaseIdentifier.Parse(version);

        return new SemanticVersion(version.Major, version.Minor, version.Patch, prereleaseIdentifier.ApplyLabel(newPrereleaseLabel).BuildPrereleaseLabels(), null);
    }

    public static SemanticVersion IncrementPatchVersion(this SemanticVersion version)
    {
        if (version.IsPrerelease)
        {
            var prereleaseIdentifier = PrereleaseIdentifier.Parse(version);
            return version.IncrementPrerelease(prereleaseIdentifier.Label);
        }
        else
        {
            return new SemanticVersion(version.Major, version.Minor, version.Patch + 1);
        }
    }
}
