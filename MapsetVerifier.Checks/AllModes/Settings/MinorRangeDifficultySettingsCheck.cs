using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.Settings;

/// <summary>
///     Base for mode-specific difficulty settings checks that only need a plain min/max guideline
///     range per setting/difficulty, reported as a single Minor issue with a shared message format.
///     Subclasses only need to supply <see cref="RangeDifficultySettingsCheck.Settings" /> and
///     <see cref="BeatmapCheck.GetMetadata" /> (Modes, Difficulties, Documentation).
/// </summary>
public abstract class MinorRangeDifficultySettingsCheck : RangeDifficultySettingsCheck
{
    public override Dictionary<string, IssueTemplate> GetTemplates() =>
        new()
        {
            {
                "outOfRange",
                new IssueTemplate(
                    Issue.Level.Minor,
                    "{0} is recommended to be {1} for {2} difficulties, currently {3}.",
                    "setting",
                    "range",
                    "difficulty",
                    "current"
                ).WithCause(
                    "A difficulty setting is outside of the recommended guideline range for this difficulty's name."
                )
            },
        };

    protected override Issue BuildIssue(
        Beatmap beatmap,
        string setting,
        SettingRange range,
        Beatmap.Difficulty difficulty,
        double current
    ) =>
        new Issue(
            GetTemplate("outOfRange"),
            beatmap,
            setting,
            FormatRange(range),
            beatmap.GetModeDifficultyName(difficulty),
            current
        ).ForDifficulties(difficulty);
}
