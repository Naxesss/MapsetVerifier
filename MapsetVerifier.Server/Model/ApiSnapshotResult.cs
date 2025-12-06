using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Snapshots;

namespace MapsetVerifier.Server.Model;

/// <summary>
/// Represents the complete snapshot comparison result for a beatmap set.
/// Organized like git commits - each snapshot version shows changes from the previous version.
/// </summary>
public readonly struct ApiSnapshotResult(
    string? title,
    string? artist,
    string? creator,
    IEnumerable<ApiSnapshotDifficulty> difficulties,
    ApiSnapshotHistory? general,
    IEnumerable<ApiSnapshotHistory> beatmapHistories,
    string? errorMessage)
{
    /// <summary>
    /// The title of the beatmap set.
    /// </summary>
    public string? Title { get; } = title;

    /// <summary>
    /// The artist of the beatmap set.
    /// </summary>
    public string? Artist { get; } = artist;

    /// <summary>
    /// The creator of the beatmap set.
    /// </summary>
    public string? Creator { get; } = creator;

    /// <summary>
    /// The difficulties (beatmap versions) in the set.
    /// </summary>
    public IEnumerable<ApiSnapshotDifficulty> Difficulties { get; } = difficulties;

    /// <summary>
    /// General/Files snapshot history (null if no beatmapset ID).
    /// </summary>
    public ApiSnapshotHistory? General { get; } = general;

    /// <summary>
    /// Snapshot histories for each beatmap difficulty.
    /// </summary>
    public IEnumerable<ApiSnapshotHistory> BeatmapHistories { get; } = beatmapHistories;

    /// <summary>
    /// Error message if snapshots could not be generated (e.g., unsubmitted map).
    /// </summary>
    public string? ErrorMessage { get; } = errorMessage;
}

/// <summary>
/// Represents a difficulty tab in the snapshot view.
/// </summary>
public readonly struct ApiSnapshotDifficulty(
    string name,
    bool isGeneral,
    double? starRating = null,
    Beatmap.Mode? mode = null)
{
    /// <summary>
    /// The difficulty name (version) or "General" for files.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Whether this is the general/files category.
    /// </summary>
    public bool IsGeneral { get; } = isGeneral;

    /// <summary>
    /// The star rating of the difficulty (null for General).
    /// </summary>
    public double? StarRating { get; } = starRating;

    /// <summary>
    /// The game mode of the difficulty (null for General).
    /// </summary>
    public Beatmap.Mode? Mode { get; } = mode;
}

/// <summary>
/// Represents the complete snapshot history for a difficulty (like git log).
/// </summary>
public readonly struct ApiSnapshotHistory(
    string difficultyName,
    IEnumerable<ApiSnapshotCommit> commits)
{
    /// <summary>
    /// The difficulty name this history represents.
    /// </summary>
    public string DifficultyName { get; } = difficultyName;

    /// <summary>
    /// The list of commits (snapshot versions), ordered from newest to oldest.
    /// </summary>
    public IEnumerable<ApiSnapshotCommit> Commits { get; } = commits;
}

/// <summary>
/// Represents a single snapshot version (like a git commit).
/// Contains all changes from the previous snapshot to this one.
/// </summary>
public readonly struct ApiSnapshotCommit(
    DateTime date,
    string id,
    int totalChanges,
    int additions,
    int removals,
    int modifications,
    IEnumerable<ApiSnapshotSection> sections)
{
    /// <summary>
    /// The date when this snapshot was created.
    /// </summary>
    public DateTime Date { get; } = date;

    /// <summary>
    /// Unique identifier for this commit (based on date).
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// Total number of changes in this commit.
    /// </summary>
    public int TotalChanges { get; } = totalChanges;

    /// <summary>
    /// Number of additions in this commit.
    /// </summary>
    public int Additions { get; } = additions;

    /// <summary>
    /// Number of removals in this commit.
    /// </summary>
    public int Removals { get; } = removals;

    /// <summary>
    /// Number of modifications in this commit.
    /// </summary>
    public int Modifications { get; } = modifications;

    /// <summary>
    /// The sections containing diffs for this commit.
    /// </summary>
    public IEnumerable<ApiSnapshotSection> Sections { get; } = sections;
}

/// <summary>
/// Represents a section of diffs (e.g., "General", "Metadata").
/// </summary>
public readonly struct ApiSnapshotSection(
    string name,
    Snapshotter.DiffType aggregatedDiffType,
    int additions,
    int removals,
    int modifications,
    IEnumerable<ApiSnapshotDiff> diffs)
{
    /// <summary>
    /// The section name (e.g., "General", "Metadata", "Difficulty").
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The aggregated diff type for the section icon.
    /// </summary>
    public Snapshotter.DiffType AggregatedDiffType { get; } = aggregatedDiffType;

    /// <summary>
    /// Number of additions in this section.
    /// </summary>
    public int Additions { get; } = additions;

    /// <summary>
    /// Number of removals in this section.
    /// </summary>
    public int Removals { get; } = removals;

    /// <summary>
    /// Number of modifications in this section.
    /// </summary>
    public int Modifications { get; } = modifications;

    /// <summary>
    /// The individual diffs in this section.
    /// </summary>
    public IEnumerable<ApiSnapshotDiff> Diffs { get; } = diffs;
}

/// <summary>
/// Represents an individual diff instance in unified diff format.
/// </summary>
public readonly struct ApiSnapshotDiff(
    string message,
    Snapshotter.DiffType diffType,
    string? oldValue,
    string? newValue,
    IEnumerable<string> details)
{
    /// <summary>
    /// The diff message describing what changed.
    /// </summary>
    public string Message { get; } = message;

    /// <summary>
    /// The type of diff (Added, Removed, Changed).
    /// </summary>
    public Snapshotter.DiffType DiffType { get; } = diffType;

    /// <summary>
    /// The old value (for Changed/Removed diffs).
    /// </summary>
    public string? OldValue { get; } = oldValue;

    /// <summary>
    /// The new value (for Changed/Added diffs).
    /// </summary>
    public string? NewValue { get; } = newValue;

    /// <summary>
    /// Additional details about the diff.
    /// </summary>
    public IEnumerable<string> Details { get; } = details;
}

