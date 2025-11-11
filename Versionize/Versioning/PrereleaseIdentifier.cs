using NuGet.Versioning;
using Versionize.CommandLine;

namespace Versionize.Versioning;

public sealed class PrereleaseIdentifier
{
    public string Label { get; }
    public int Number { get; }

    private PrereleaseIdentifier(string label, int number)
    {
        Label = label;
        Number = number;
    }

    public PrereleaseIdentifier ApplyLabel(string label)
    {
        if (Label.Equals(label))
        {
            return new PrereleaseIdentifier(label, Number + 1);
        }

        return new PrereleaseIdentifier(label, 0);
    }

    public string[] BuildPrereleaseLabels()
    {
        return [Label, Number.ToString()];
    }

    public static PrereleaseIdentifier Parse(SemanticVersion version)
    {
        var releaseLabels = version.ReleaseLabels.ToArray();
        var prereleaseLabel = releaseLabels[0];
        if (!int.TryParse(releaseLabels[1], out var prereleaseNumber))
        {
            throw new VersionizeException(ErrorMessages.PrereleaseNumberParseError(version.ToNormalizedString()), 1);
        }

        return new PrereleaseIdentifier(prereleaseLabel, prereleaseNumber);
    }
}
