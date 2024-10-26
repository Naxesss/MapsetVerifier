using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.Settings
{
    [Check]
    public class CheckDefaultColours : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = new[]
                {
                    // Does not apply to taiko, due to always using red/blue.
                    // Does not apply to mania, due to not having combo colours (based on column instead).
                    Beatmap.Mode.Standard,
                    Beatmap.Mode.Catch
                },
                Category = "Settings",
                Message = "Default combo colours without forced skin.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Preventing the combo colours chosen without additional input from blending into the background.
                    <image-right>
                        https://i.imgur.com/G5vTU7f.png
                        The combo colour section in song setup without custom colours ticked.
                    </image>"
                    },
                    {
                        "Reasoning",
                        @"
                    If you leave the combo colour setting as it is when you create a beatmap, no [Colours] section will 
                    be created in the .osu, meaning the skins of users will override them. Since we can't control which 
                    colours they may use or force them to dim the background, the colours may blend into the background 
                    making for an unfair gameplay experience.
                    <br \><br \>
                    If you set a preferred skin in the beatmap however, for example default, that skin will be used over 
                    any user skin, but many players switch skins to get away from default, so would not recommend this.
                    If you want the default colours, simply tick the ""Enable Custom Colours"" checkbox instead."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Default",
                    new IssueTemplate(Issue.Level.Problem, "Default combo colours without preferred skin.").WithCause("A beatmap has no custom combo colours and does not have any preferred skin.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            if (beatmap.GeneralSettings.skinPreference != "Default" && !beatmap.ColourSettings.combos.Any())
                yield return new Issue(GetTemplate("Default"), beatmap);
        }
    }
}