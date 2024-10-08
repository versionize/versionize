using NuGet.Versioning;

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
        try
        {
            var releaseLabels = version.ReleaseLabels.ToArray();

            var prereleaseLabel = releaseLabels[0];
            var prereleaseNumber = int.Parse(releaseLabels[1]);

            return new PrereleaseIdentifier(prereleaseLabel, prereleaseNumber);
        }
        catch (Exception e)
        {
            throw new InvalidPrereleaseIdentifierException($"Could not parse prerelease labels of version {version}. Expected format: <label>.<number>", e);
        }
    }
}
