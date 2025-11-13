namespace Versionize.Config;

public sealed class ChangelogSection
{
    public string? Type { get; init; }
    public string? Section { get; init; }
    public bool Hidden { get; init; }
}
