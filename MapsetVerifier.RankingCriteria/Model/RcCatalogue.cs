using System.Text.Json.Serialization;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.RankingCriteria.Model;

public enum RcKind
{
    Rule,
    Guideline,
    Allowance,
}

/// <summary> Whether a statement can be verified by a check, decided by a human. </summary>
public enum RcAutomation
{
    /// <summary> Nobody has looked at this statement yet. </summary>
    Unknown,

    /// <summary> A check can fully verify this statement. </summary>
    Automatable,

    /// <summary> A check can verify some, but not all, of this statement. </summary>
    Partial,

    /// <summary> The statement requires human judgement, e.g. whether something fits the music. </summary>
    Manual,
}

/// <summary> The osu-wiki commit a snapshot was taken from. </summary>
public sealed class RcSource
{
    public string Repository { get; set; } = "ppy/osu-wiki";
    public string Commit { get; set; } = "";
    public DateTimeOffset? CommitDate { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
}

/// <summary> All statements parsed from one ranking criteria page. </summary>
public sealed class RcCataloguePage
{
    public string Page { get; set; } = "";
    public List<RcStatement> Statements { get; set; } = [];
}

/// <summary> A single rule, guideline or allowance of the ranking criteria. </summary>
public sealed class RcStatement
{
    /// <summary>
    ///     Stable identifier, e.g. "osu/hit-objects-never-off-screen". Assigned once and never derived again,
    ///     so it may be renamed by hand in the catalogue; the sync matches statements by fingerprint instead.
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary> Everything parsed from the wiki. Rewritten on every sync. </summary>
    public RcUpstream Upstream { get; set; } = new();

    /// <summary> Everything decided by humans. Never touched by the sync. </summary>
    public RcCuration Curation { get; set; } = new();

    /// <summary> The commit at which this statement disappeared from the wiki, if it did. </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RetiredAt { get; set; }

    [JsonIgnore]
    public bool IsRetired => RetiredAt != null;
}

public sealed class RcUpstream
{
    /// <summary> Headings above the statement, excluding the page title and the Rules/Guidelines/Allowances heading. </summary>
    public List<string> Path { get; set; } = [];

    public RcKind Kind { get; set; }

    /// <summary> The leading sentence with markdown removed, used for display and matching. </summary>
    public string Lead { get; set; } = "";

    /// <summary> Normalized lead used to recognize the statement across wiki edits. </summary>
    public string Fingerprint { get; set; } = "";

    /// <summary> Hash of the full statement text, excluding nested statements. Changes whenever its wording does. </summary>
    public string BodyHash { get; set; } = "";

    /// <summary> Heading anchor on the osu! wiki page closest to the statement. </summary>
    public string Anchor { get; set; } = "";

    /// <summary> 1-based line range in the snapshot's markdown file, excluding nested statements. </summary>
    public int StartLine { get; set; }

    public int EndLine { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Parent { get; set; }

    /// <summary>
    ///     Whether this only opens a sentence its nested statements finish, as in "The audio file of a beatmap must...".
    ///     Intros are not rules themselves, so they are neither linked nor counted towards coverage.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Intro { get; set; }

    /// <summary> Difficulty levels this statement is limited to, empty when it applies to all of them. </summary>
    public List<Beatmap.Difficulty> Difficulties { get; set; } = [];
}

public sealed class RcCuration
{
    public RcAutomation Automation { get; set; } = RcAutomation.Unknown;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Notes { get; set; }
}
