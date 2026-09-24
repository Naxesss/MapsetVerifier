using MapsetVerifier.RankingCriteria.Model;
using MapsetVerifier.RankingCriteria.Parsing;

namespace MapsetVerifier.RankingCriteria.Sync;

public enum RcChangeType
{
    Added,
    Reworded,
    Edited,
    Moved,
    KindChanged,
    Retired,
    Revived,
}

public sealed record RcChange(RcChangeType Type, string Id, string Detail);

public sealed record RcMergeResult(RcCataloguePage Page, List<RcChange> Changes);

/// <summary>
///     Matches freshly parsed statements to the ids already in the catalogue, so ids and curation survive wiki edits.
///     <para />
///     1. Same fingerprint anywhere on the page → same statement (moved if its headings changed).
///     <br />
///     2. Similar wording, preferring the same section → reworded statement.
///     <br />
///     3. Anything left is new and gets a drafted id; old statements left unmatched are retired.
/// </summary>
public static class RcCatalogueMerger
{
    /// <summary> Minimum word overlap for a changed lead to still count as the same statement. </summary>
    public const double RewordThreshold = 0.6;

    public static RcMergeResult Merge(
        RcPage page,
        RcCataloguePage? existing,
        List<ParsedStatement> parsed,
        string commit
    )
    {
        var changes = new List<RcChange>();
        var previous = existing?.Statements ?? [];
        var claimed = new HashSet<RcStatement>();
        var matches = new RcStatement?[parsed.Count];

        // 1. Exact fingerprint. Prefer the same position in the heading tree when a lead occurs more than once.
        for (var i = 0; i < parsed.Count; ++i)
        {
            var candidate = previous
                .Where(statement => !claimed.Contains(statement))
                .Where(statement =>
                    statement.Upstream.Fingerprint == parsed[i].Upstream.Fingerprint
                )
                .OrderByDescending(statement =>
                    statement.Upstream.Path.SequenceEqual(parsed[i].Upstream.Path)
                )
                .FirstOrDefault();

            if (candidate == null)
                continue;

            matches[i] = candidate;
            claimed.Add(candidate);
        }

        // 2. Reworded leads.
        for (var i = 0; i < parsed.Count; ++i)
        {
            if (matches[i] != null)
                continue;

            var best = previous
                .Where(statement => !claimed.Contains(statement) && !statement.IsRetired)
                .Select(statement =>
                    (
                        Statement: statement,
                        Score: RcText.Similarity(
                            statement.Upstream.Fingerprint,
                            parsed[i].Upstream.Fingerprint
                        )
                            // Nudge towards the same section so similar rules of two difficulties do not swap.
                            + (
                                statement.Upstream.Path.SequenceEqual(parsed[i].Upstream.Path)
                                    ? 0.05
                                    : 0
                            )
                    )
                )
                .Where(pair => pair.Score >= RewordThreshold)
                .OrderByDescending(pair => pair.Score)
                .FirstOrDefault();

            if (best.Statement == null)
                continue;

            matches[i] = best.Statement;
            claimed.Add(best.Statement);
        }

        var usedIds = previous.Select(statement => statement.Id).ToHashSet();
        var result = new List<RcStatement>();

        for (var i = 0; i < parsed.Count; ++i)
        {
            var upstream = parsed[i].Upstream;
            var match = matches[i];

            if (match == null)
            {
                var id = UniqueId(DraftId(page, parsed[i]), usedIds);
                usedIds.Add(id);
                result.Add(new RcStatement { Id = id, Upstream = upstream });
                changes.Add(new RcChange(RcChangeType.Added, id, upstream.Lead));
                continue;
            }

            var old = match.Upstream;

            if (match.IsRetired)
                changes.Add(new RcChange(RcChangeType.Revived, match.Id, upstream.Lead));
            else if (old.Fingerprint != upstream.Fingerprint)
                changes.Add(
                    new RcChange(
                        RcChangeType.Reworded,
                        match.Id,
                        $"\"{old.Lead}\" → \"{upstream.Lead}\""
                    )
                );
            else if (old.BodyHash != upstream.BodyHash)
                changes.Add(new RcChange(RcChangeType.Edited, match.Id, upstream.Lead));

            if (!old.Path.SequenceEqual(upstream.Path))
                changes.Add(
                    new RcChange(
                        RcChangeType.Moved,
                        match.Id,
                        $"{string.Join(" › ", old.Path)} → {string.Join(" › ", upstream.Path)}"
                    )
                );

            if (old.Kind != upstream.Kind && !match.IsRetired)
                changes.Add(
                    new RcChange(
                        RcChangeType.KindChanged,
                        match.Id,
                        $"{old.Kind} → {upstream.Kind}"
                    )
                );

            result.Add(
                new RcStatement
                {
                    Id = match.Id,
                    Upstream = upstream,
                    Curation = match.Curation,
                }
            );
        }

        // Parents are only known by index while parsing.
        for (var i = 0; i < parsed.Count; ++i)
        {
            var parentIndex = parsed[i].ParentIndex;
            result[i].Upstream.Parent = parentIndex is { } index ? result[index].Id : null;
        }

        foreach (var statement in previous.Where(statement => !claimed.Contains(statement)))
        {
            if (!statement.IsRetired)
            {
                statement.RetiredAt = commit;
                changes.Add(
                    new RcChange(RcChangeType.Retired, statement.Id, statement.Upstream.Lead)
                );
            }

            result.Add(statement);
        }

        return new RcMergeResult(
            new RcCataloguePage { Page = page.Key, Statements = result },
            changes
        );
    }

    /// <summary>
    ///     "osu/hit-objects-never-off-screen" for general sections, "catch/salad/edge-dashes-not-used" for others.
    /// </summary>
    private static string DraftId(RcPage page, ParsedStatement statement)
    {
        var section = RcText.Anchor(statement.Section);
        var slug = RcText.Slug(statement.Upstream.Lead);

        return section is "" or "general" || section == page.Key
            ? $"{page.Key}/{slug}"
            : $"{page.Key}/{section}/{slug}";
    }

    private static string UniqueId(string id, HashSet<string> usedIds)
    {
        if (!usedIds.Contains(id))
            return id;

        for (var suffix = 2; ; ++suffix)
        {
            var candidate = $"{id}-{suffix}";
            if (!usedIds.Contains(candidate))
                return candidate;
        }
    }
}
