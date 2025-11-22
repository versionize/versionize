namespace Versionize.Config;

public sealed record class VersioningOptions
{
    public IEnumerable<VersioningPosition>? Positions { get; init; }
}

public sealed record class VersioningPosition
{
    public required string Type { get; init; }
    public required int Position { get; init; }
}
