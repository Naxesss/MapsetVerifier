using System.Numerics;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.AllModes.Settings
{
    [Check]
    public class CheckLuminosity : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes =
                [
                    // Does not apply to taiko, due to always using red/blue.
                    // Does not apply to mania, due to not having combo colours (based on column instead).
                    Beatmap.Mode.Standard,
                    Beatmap.Mode.Catch
                ],
                Category = "Settings",
                Message = "Too dark or bright combo colours or slider borders.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Preventing combo colours from blending into dimmed backgrounds or flashing too intensely in kiai."
                    },
                    {
                        "Reasoning",
                        @"
                        Although objects by default have a white border around them making them visible, the approach circles are 
                        affected by combo colour and will become impossible to see with colour 0,0,0. Stripping the game of 
                        important gameplay indicators or generally messing with them (see check for modified breaks) is not 
                        something beatmaps are expected to do, as they need to be consistent to work properly.

                        ![](https://i.imgur.com/wxoMMQG.png)
                        A slider whose approach circle is only visible on its borders and path, due to the rest blending into the dimmed bg.

                        As for bright colours, when outside of kiai they're fine, but while in kiai the game flashes them, 
                        attempting to make them even brighter without caring about them already being really bright, resulting in 
                        pretty strange behaviour for some monitors and generally just unpleasant contrast.
                        ![](https://i.imgur.com/9cRTvJc.png)
                        An example of a slider with colour 255,255,255 while in the middle of flashing.

                        This check uses the [HSP colour system](https://alienryderflex.com/hsp.html) to better approximate 
                        the way humans perceive luminosity in colours, as opposed to the HSL system where green is regarded the same 
                        luminosity as deep blue, see image.
                        ![](https://i.imgur.com/CjPhf0b.png)
                        The HSP colour system compared to the in-game HSL system."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Problem Combo",
                    new IssueTemplate(Issue.Level.Problem, "Combo colour {0} is way too dark.", "number")
                        .WithCause("The HSP luminosity value of a combo colour is lower than 30. These values are visible in the overview section as tooltips for each colour if you want to check them manually.")
                },

                {
                    "Warning Combo",
                    new IssueTemplate(Issue.Level.Warning, "Combo colour {0} is really dark.", "number")
                        .WithCause("Same as the first check, but lower than 43 instead.")
                },

                {
                    "Problem Border",
                    new IssueTemplate(Issue.Level.Problem, "Slider border is way too dark.")
                        .WithCause("Same as the first check, except applies on the slider border instead.")
                },

                {
                    "Warning Border",
                    new IssueTemplate(Issue.Level.Warning, "Slider border is really dark.")
                        .WithCause("Same as the second check, except applies on the slider border instead.")
                },

                {
                    "Bright",
                    new IssueTemplate(Issue.Level.Warning, "Combo colour {0} is really bright in kiai sections, see {1}.", "number", "example object")
                        .WithCause("Same as the first check, but higher than 250 and requires that at least one hit object with the combo is in a kiai section.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            // Luminosity thresholds, ~15 more lenient than the original 53 / 233
            const float luminosityMinRankable = 30;
            const float luminosityMinWarning = 43;
            const float luminosityMax = 250;

            if (beatmap.ColourSettings.sliderBorder != null)
            {
                var colour = beatmap.ColourSettings.sliderBorder.GetValueOrDefault();
                var luminosity = GetLuminosity(colour);

                if (luminosity < luminosityMinRankable)
                    yield return new Issue(GetTemplate("Problem Border"), beatmap);
                else if (luminosity < luminosityMinWarning)
                    yield return new Issue(GetTemplate("Warning Border"), beatmap);
            }

            var comboColoursInKiai = new List<int>();
            var comboColourTime = new List<double>();

            foreach (var hitObject in beatmap.HitObjects)
            {
                var combo = beatmap.GetComboColourIndex(hitObject.time);
                var timingLine = beatmap.GetTimingLine(hitObject.time);
                if (timingLine == null)
                {
                    // No need to check further if there's no timing line in the beatmap
                    break;
                }

                // Spinners don't have a colour.
                if (hitObject is Spinner || !timingLine.Kiai || comboColoursInKiai.Contains(combo))
                    continue;

                comboColoursInKiai.Add(combo);
                comboColourTime.Add(hitObject.time);
            }

            for (var i = 0; i < beatmap.ColourSettings.combos.Count; ++i)
            {
                var colour = beatmap.ColourSettings.combos.ElementAt(i);
                var luminosity = GetLuminosity(colour);

                var displayedColourIndex = beatmap.AsDisplayedComboColourIndex(i);

                if (luminosity < luminosityMinRankable)
                    yield return new Issue(GetTemplate("Problem Combo"), beatmap, displayedColourIndex);

                else if (luminosity < luminosityMinWarning)
                    yield return new Issue(GetTemplate("Warning Combo"), beatmap, displayedColourIndex);

                for (var j = 0; j < comboColoursInKiai.Count; ++j)
                    if (luminosity > luminosityMax && comboColoursInKiai[j] == i)
                        yield return new Issue(GetTemplate("Bright"), beatmap, displayedColourIndex, Timestamp.Get(comboColourTime[j]));
            }
        }

        private static float GetLuminosity(Vector3 colour) =>
            // HSP colour model http://alienryderflex.com/hsp.html
            (float)Math.Sqrt(colour.X * colour.X * 0.299f + colour.Y * colour.Y * 0.587f + colour.Z * colour.Z * 0.114f);
    }
}