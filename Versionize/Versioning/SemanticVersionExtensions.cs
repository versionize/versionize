using NuGet.Versioning;

namespace Versionize.Versioning;

public static class SemanticVersionExtensions
{
    public static SemanticVersion AsPrerelease(this SemanticVersion version, string prereleaseLabel, int prereleaseNumber)
    {
        return new SemanticVersion(version.Major, version.Minor, version.Patch, [prereleaseLabel, prereleaseNumber.ToString()], null);
    }

    public static SemanticVersion AsRelease(this SemanticVersion version)
    {
        return new SemanticVersion(version.Major, version.Minor, version.Patch);
    }

    public static SemanticVersion IncrementPrerelease(this SemanticVersion version, string newPrereleaseLabel)
    {
        var prereleaseIdentifier = PrereleaseIdentifier.Parse(version);
        var prereleaseLabels = prereleaseIdentifier.ApplyLabel(newPrereleaseLabel).BuildPrereleaseLabels();

        return new SemanticVersion(version.Major, version.Minor, version.Patch, prereleaseLabels, null);
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
