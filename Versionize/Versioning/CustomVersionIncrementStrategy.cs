using Versionize.Config;
using Versionize.ConventionalCommits;

namespace Versionize.Versioning;

public sealed class CustomVersionIncrementStrategy(IEnumerable<ConventionalCommit> conventionalCommits, VersioningOptions config)
{
    private readonly IEnumerable<ConventionalCommit> _conventionalCommits = conventionalCommits;
    private readonly Dictionary<string, int> _typeToPosition = (config.Positions ?? [])
        .GroupBy(p => p.Type)
        .ToDictionary(g => g.Key, g => g.First().Position, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the most significant position to bump (1-based), based on the provided commits and mapping.
    /// Lower numbers indicate more significant positions. Returns null when no bump should occur.
    /// Rules:
    /// - Any breaking change forces position 1.
    /// - For mapped commit types, selects the minimal configured position found.
    /// - Unknown types contribute a bump only when insignificantCommitsAffectVersion is true; in that case, use the
    ///   least significant configured position (max position value) if available, otherwise 1.
    /// </summary>
    public int? GetPositionToBump(bool insignificantCommitsAffectVersion = true)
    {
        int? best = null; // smaller is more significant

        // Pre-compute least significant configured position to use for insignificant bumps
        int? leastSignificant = _typeToPosition.Count > 0 ? _typeToPosition.Values.Max() : 1;

        foreach (var commit in _conventionalCommits)
        {
            if (commit.IsBreakingChange)
            {
                return 1;
            }

            var type = commit.Type;
            if (string.IsNullOrWhiteSpace(type))
            {
                if (insignificantCommitsAffectVersion && leastSignificant.HasValue)
                {
                    best = best is null ? leastSignificant : Math.Min(best.Value, leastSignificant.Value);
                }
                continue;
            }

            if (_typeToPosition.TryGetValue(type, out var position))
            {
                best = best is null ? position : Math.Min(best.Value, position);
            }
            else if (insignificantCommitsAffectVersion && leastSignificant.HasValue)
            {
                best = best is null ? leastSignificant : Math.Min(best.Value, leastSignificant.Value);
            }
        }

        return best;
    }
}
