using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.Settings;

/// <summary>
///     Common plumbing shared by checks that flag AR/OD/HP/CS values which don't line up with the
///     guideline settings for a difficulty's name. Handles rounding of raw settings values consistently.
/// </summary>
public abstract class DifficultySettingsCheck : BeatmapCheck
{
    protected static double Round(float value) => Math.Round(value, 2, MidpointRounding.ToEven);
}
