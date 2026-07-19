using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using static MapsetVerifier.Checks.Utils.TaikoUtils;

namespace MapsetVerifier.Checks.Taiko.Compose
{
    [Check]
    public class CheckRestMoments : BeatmapCheck
    {
        private const string Minor = nameof(Issue.Level.Minor);
        private const string Warning = nameof(Issue.Level.Warning);

        private readonly Beatmap.Difficulty[] difficulties =
        [
            Beatmap.Difficulty.Easy,
            Beatmap.Difficulty.Normal,
            Beatmap.Difficulty.Hard,
            Beatmap.Difficulty.Insane,
        ];

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata()
            {
                Author = "Hivie, Phob, Nostril",
                Category = "Compose",
                Message = "Rest moments",

                Difficulties = difficulties,

                Modes = [Beatmap.Mode.Taiko],

                Documentation = new Dictionary<string, string>()
                {
                    {
                        "Purpose",
                        @"
                    Rest moments are small pauses in gameplay used to separate patterns and provide a break for the player from continuous rhythm chains. They play an important role in reducing player fatigue, especially on denser rhythms.

                    Using rest moments less frequently is acceptable if either the pace of the music makes rest moments counter-intuitive or if the continuously mapped part is overall more forgiving to the player.

                    **Each difficulty should follow its respective rest moment guidelines:**

                    | Difficulty | Rest moment | Example | Chain length |
                    | :-: | :-: | :-: | :-- |
                    | **Kantan** | 3/1 or longer | ![](assets/checks/taiko-compose-rest-moments-example-kantan.png ""3/1"") | Rest moment needed every 32–36 beats |
                    | **Futsuu** | 2/1 or longer | ![](assets/checks/taiko-compose-rest-moments-example-futsuu.png ""2/1"") | Rest moment needed every 32–36 beats |
                    | **Muzukashii** (option 1) | 3/2 or longer | ![](assets/checks/taiko-compose-rest-moments-example-muzu-3-2.png ""3/2"") | Rest moment needed every 32–36 beats |
                    | **Muzukashii** (option 2) | 3 consecutive 1/1 or longer | ![](assets/checks/taiko-compose-rest-moments-example-muzu-1-1.png ""3 consecutive 1/1"") | Rest moment needed every 32–36 beats |
                    | **Oni** | 1/1 or longer | ![](assets/checks/taiko-compose-rest-moments-example-oni.png ""1/1"") | Rest moment needed every 16–20 beats"
                    },
                    {
                        "Reasoning",
                        @"
                    Abnormally long chains without proper rest moments can be very straining to play."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    Minor,
                    new IssueTemplate(
                        Issue.Level.Minor,
                        "{0} > {1} No {2} rest moments for {3}. Ensure this makes sense.",
                        "start",
                        "end",
                        "break",
                        "length"
                    ).WithCause("Chain length is surpassing the RC guideline, but not excessively")
                },
                {
                    Warning,
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} > {1} No {2} rest moments for {3}. Ensure this makes sense.",
                        "start",
                        "end",
                        "break",
                        "length"
                    ).WithCause("Chain length is excessively surpassing the RC guideline")
                },
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var objects = beatmap.HitObjects.ToList();

            // for each diff: acceptable versions of { # of consecutive gaps required, number of beats required per gap }
            var minimalGapBeats = new Dictionary<Beatmap.Difficulty, Dictionary<int, double>>()
            {
                {
                    Beatmap.Difficulty.Easy,
                    new Dictionary<int, double>() { { 1, 3.0 } }
                },
                {
                    Beatmap.Difficulty.Normal,
                    new Dictionary<int, double>() { { 1, 2.0 } }
                },
                {
                    Beatmap.Difficulty.Hard,
                    new Dictionary<int, double>() { { 1, 1.5 }, { 3, 1.0 } }
                },
                {
                    Beatmap.Difficulty.Insane,
                    new Dictionary<int, double>() { { 1, 1.0 } }
                },
            };

            // for each diff: string to output describing rest moment requirements
            var breakTypes = new Dictionary<Beatmap.Difficulty, string>()
            {
                { Beatmap.Difficulty.Easy, "3/1" },
                { Beatmap.Difficulty.Normal, "2/1" },
                { Beatmap.Difficulty.Hard, "3/2 or 3 x 1/1" },
                { Beatmap.Difficulty.Insane, "1/1" },
            };

            // for each diff: string to output describing continuous mapping limitations (minor issue severity)
            var continuousMappingMinorLimit = new Dictionary<Beatmap.Difficulty, double>()
            {
                { Beatmap.Difficulty.Easy, 36 },
                { Beatmap.Difficulty.Normal, 36 },
                { Beatmap.Difficulty.Hard, 36 },
                { Beatmap.Difficulty.Insane, 20 },
            };

            // for each diff: string to output describing continuous mapping limitations (warning severity)
            var continuousMappingWarningLimit = new Dictionary<Beatmap.Difficulty, double>()
            {
                { Beatmap.Difficulty.Easy, 44 },
                { Beatmap.Difficulty.Normal, 44 },
                { Beatmap.Difficulty.Hard, 44 },
                { Beatmap.Difficulty.Insane, 32 },
            };

            foreach (var diff in difficulties)
            {
                var currentContinuousSectionStartTimeMs = objects.FirstOrDefault()?.time ?? 0;
                var isWithinContinuousMapping = false;
                for (int i = 0; i < objects.Count; i++)
                {
                    var current = objects[i];
                    var timing = beatmap.GetTimingLine<UninheritedLine>(current.time);
                    if (timing == null)
                    {
                        continue;
                    }

                    var normalizedMsPerBeat = timing.GetNormalizedMsPerBeat();

                    // identify boundaries of continuous mapping
                    var isBeginningOfContinuousMapping = false;
                    var isEndOfContinuousMapping = false;
                    foreach (var acceptableRestMoment in minimalGapBeats[diff])
                    {
                        // convert minimal gap beats to milliseconds
                        var minimalRestMomentGapMs =
                            acceptableRestMoment.Value * normalizedMsPerBeat;

                        // check if this is beginning of continuous mapping
                        var smallestConsecutiveGapMs = double.MaxValue;
                        for (int j = 0; j < acceptableRestMoment.Key; j++)
                        {
                            // out of bounds check
                            if (i - j - 1 < 0)
                            {
                                continue;
                            }

                            var gapBeginObject = objects[i - j - 1];
                            var gapEndObject = objects[i - j];

                            var gap = gapEndObject.time - gapBeginObject.GetEndTime();
                            smallestConsecutiveGapMs = Math.Min(smallestConsecutiveGapMs, gap);
                        }

                        // check if the backward-looking current string of notes is a rest moment
                        if (smallestConsecutiveGapMs + Common.MS_EPSILON >= minimalRestMomentGapMs)
                        {
                            isBeginningOfContinuousMapping = true;
                        }

                        // check if this is end of continuous mapping
                        smallestConsecutiveGapMs = double.MaxValue;
                        for (int j = 0; j < acceptableRestMoment.Key; j++)
                        {
                            // out of bounds check
                            if (i + j + 1 >= objects.Count)
                            {
                                continue;
                            }

                            var gapBeginObject = objects[i + j];
                            var gapEndObject = objects[i + j + 1];

                            var gap = gapEndObject.time - gapBeginObject.GetEndTime();
                            smallestConsecutiveGapMs = Math.Min(smallestConsecutiveGapMs, gap);
                        }

                        // check if the forward-looking current string of notes is a rest moment
                        if (smallestConsecutiveGapMs + Common.MS_EPSILON >= minimalRestMomentGapMs)
                        {
                            isEndOfContinuousMapping = true;
                        }
                    }

                    // check if this is the first note of a continuously mapped section, if so record the timestamp for later once we find the end
                    if (isBeginningOfContinuousMapping)
                    {
                        isWithinContinuousMapping = true;
                        currentContinuousSectionStartTimeMs = current.time;
                    }

                    // check if this is the last note of a continuously mapped section, if so check if it's too long
                    if (isEndOfContinuousMapping && isWithinContinuousMapping)
                    {
                        isWithinContinuousMapping = false;
                        var continuouslyMappedDurationMs =
                            current.GetEndTime() - currentContinuousSectionStartTimeMs;

                        double beatsWithoutBreaks = Math.Floor(
                            (continuouslyMappedDurationMs + Common.MS_EPSILON) / timing.msPerBeat
                        );

                        if (beatsWithoutBreaks > continuousMappingWarningLimit[diff])
                        {
                            yield return new Issue(
                                GetTemplate(Warning),
                                beatmap,
                                Timestamp.Get(currentContinuousSectionStartTimeMs).Trim(),
                                Timestamp.Get(current.GetEndTime()),
                                breakTypes[diff],
                                $"{beatsWithoutBreaks}/1"
                            ).ForDifficulties(diff);
                        }
                        else if (beatsWithoutBreaks > continuousMappingMinorLimit[diff])
                        {
                            yield return new Issue(
                                GetTemplate(Minor),
                                beatmap,
                                Timestamp.Get(currentContinuousSectionStartTimeMs).Trim(),
                                Timestamp.Get(current.GetEndTime()),
                                breakTypes[diff],
                                $"{beatsWithoutBreaks}/1"
                            ).ForDifficulties(diff);
                        }
                    }
                }
            }
        }
    }
}
