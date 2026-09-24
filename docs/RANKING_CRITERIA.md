# Ranking criteria

Mapset Verifier ships a snapshot of the [ranking criteria](https://osu.ppy.sh/wiki/Ranking_criteria) pages from [ppy/osu-wiki](https://github.com/ppy/osu-wiki/tree/master/wiki/Ranking_criteria). Checks link their issue templates to the statements (rules, guidelines and allowances) in that snapshot, which lets the app:

- point a user from an issue to the exact statement it enforces, highlighted in the page it belongs to;
- show which statements are covered by checks and which still need one.

## Layout

Everything lives in [`MapsetVerifier.RankingCriteria`](../MapsetVerifier.RankingCriteria):

| Path | Contents |
| --- | --- |
| `Data/source.json` | The osu-wiki commit the snapshot was taken from. |
| `Data/snapshots/<commit>/*.md` | The pages, exactly as they are in the osu-wiki (`en.md` only). |
| `Data/catalogue/<page>.json` | One entry per statement: a stable `id`, what was parsed from the wiki (`upstream`) and what humans decided (`curation`). |
| `RC.g.cs` | Generated `RC.<Page>.<Name>` constants for every statement id. Do not edit. |

The snapshot and catalogue are embedded in the assembly, so the app never needs the wiki at runtime.

`Simplified_ranking_criteria` (a summary of the other pages), `Ranking_Criteria_Council` (historical) and `Skin_set_list` (a reference list) are not part of the snapshot. `Scaling_BPM` and `Difficulty_naming` are included for viewing only.

## Linking a check

```csharp
using MapsetVerifier.RankingCriteria;

new IssueTemplate(Issue.Level.Problem, "{0} {1} is offscreen.", "timestamp -", "object")
    .WithCause("The border of a hit object is partially off the screen in 4:3 aspect ratios.")
    .WithRule(RC.Osu.HitObjectsNeverOffScreen)
```

Link templates rather than whole checks: one check often enforces a rule at one level and a guideline at another. [`RankingCriteriaLinkTests`](../MapsetVerifier.Checks.Tests/RankingCriteriaLinkTests.cs) verifies that every linked id exists and that the statement's page applies to the check's modes. A template's level does not have to match the statement's kind; a `Problem` may link to a guideline.

## Updating the snapshot

```bash
./scripts/update-ranking-criteria.sh            # latest osu-wiki commit touching wiki/Ranking_criteria
./scripts/update-ranking-criteria.sh --commit <sha>
./scripts/update-ranking-criteria.sh reparse    # re-run the parser on the current snapshot
```

(`scripts\update-ranking-criteria.ps1` on Windows.) Set `GITHUB_TOKEN` if you hit GitHub's rate limit. When the wiki has not changed since the snapshot, nothing is written.

The [Sync ranking criteria](../.github/workflows/sync-ranking-criteria.yaml) workflow runs this every Monday and opens a pull request with the change report when the ranking criteria changed. It needs "Allow GitHub Actions to create and approve pull requests" enabled in the repository settings.

The tool downloads the pages, parses them and merges the result into the catalogue, printing a report of what changed. Statements keep their id across wiki edits:

1. A statement with the same (normalized) lead sentence is the same statement, even if it moved to another section.
2. Otherwise a statement with a similar lead is treated as reworded and keeps its id. Check the report for these.
3. Anything else is new and gets a drafted id. Statements that disappeared are marked `retiredAt` and their `RC` constant is removed, so checks still linking to them fail to compile until they are re-linked.

Drafted ids look like `osu/hit-objects-never-off-screen` for general sections and `catch/salad/edge-dashes-not-used` for others. They may be renamed by hand in the catalogue: the tool matches on the lead sentence and never derives an id again.

Statements are bolded list items (or paragraphs) below a Rules, Guidelines or Allowances heading, plus plain top-level list items there, such as the difficulty setting guidelines and marker lists. Nested bold items become sub-statements; a statement without links of its own counts as covered through its sub-statements.

Set `curation.automation` to `manual` for statements that need human judgement, so they do not count as missing a check, or `partial` when a linked check only covers part of the statement.

osu-wiki content is licensed under [CC BY-NC 4.0](https://github.com/ppy/osu-wiki/blob/master/LICENCE.md).
