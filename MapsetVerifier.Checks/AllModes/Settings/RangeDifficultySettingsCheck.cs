using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.Settings;

/// <summary>
///     Base for checks that flag a difficulty setting (AR/OD/HP/CS) which falls outside of a
///     min/max guideline range for the beatmap's own difficulty. Subclasses only need to supply
///     the per-difficulty ranges for each setting, plus their metadata/documentation; this class
///     handles reading the beatmap's actual difficulty, rounding, and iterating the settings.
/// </summary>
public abstract class RangeDifficultySettingsCheck : DifficultySettingsCheck
{
    protected readonly record struct SettingRange(double? Min, double? Max);

    protected readonly record struct SettingDefinition(
        string Name,
        Func<Beatmap, float> GetValue,
        IReadOnlyDictionary<Beatmap.Difficulty, SettingRange> Ranges
    );

    /// <summary> The settings (e.g. AR/OD/HP/CS) this check evaluates, each with its own per-difficulty ranges. </summary>
    protected abstract IReadOnlyList<SettingDefinition> Settings { get; }

    /// <summary> Builds the issue for a setting found to be outside of its guideline range. </summary>
    protected abstract Issue BuildIssue(
        Beatmap beatmap,
        string setting,
        SettingRange range,
        Beatmap.Difficulty difficulty,
        double current
    );

    /// <summary> Extension point for checks that need to yield additional issues outside of the range checks, e.g. an ambiguous difficulty name. </summary>
    protected virtual IEnumerable<Issue> GetAdditionalIssues(
        Beatmap beatmap,
        Beatmap.Difficulty difficulty
    ) => [];

    public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
    {
        var difficulty = beatmap.GetDifficulty();

        foreach (var setting in Settings)
        {
            if (!setting.Ranges.TryGetValue(difficulty, out var range))
                continue;

            var current = Round(setting.GetValue(beatmap));

            if (range.Min is { } min && current < min)
                yield return BuildIssue(beatmap, setting.Name, range, difficulty, current);
            else if (range.Max is { } max && current > max)
                yield return BuildIssue(beatmap, setting.Name, range, difficulty, current);
        }

        foreach (var issue in GetAdditionalIssues(beatmap, difficulty))
            yield return issue;
    }

    protected static string FormatRange(SettingRange range)
    {
        if (range.Min is { } min && range.Max is { } max)
            return $"between {min} and {max}";

        if (range.Min is { } minOnly)
            return $"{minOnly} or higher";

        return $"{range.Max} or lower";
    }
}
