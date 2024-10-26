using System.Collections.Generic;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Standard.Spread
{
    [Check]
    public class CheckCloseOverlap : BeatmapSetCheck
    {
        private const double ProblemThreshold = 125; // Shortest acceptable gap is 1/2 in 240 BPM, 125 ms.
        private const double WarningThreshold = 188; // Shortest gap before warning is 1/2 in 160 BPM, 188 ms.

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes =
                [
                    Beatmap.Mode.Standard
                ],
                Category = "Spread",
                Message = "Objects close in time not overlapping.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Ensuring that objects close in time are indiciated as such in easy and lowest normal difficulties.
                    <image>
                        https://i.imgur.com/rnIi6Pj.png
                        Right image is harder to distinguish time distance in, despite spacings still clearly being different.
                    </image>"
                    },
                    {
                        "Reasoning",
                        @"
                    Newer players often have trouble reading how far apart objects are in time, which is why enabling 
                    distance spacing for lower difficulties is often recommended. However, if two spacings for different 
                    snappings look similar, it's possible to confuse them. By forcing an overlap between objects close in 
                    time and discouraging it for objects further apart, the difference in snappings become more apparent."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Problem",
                    new IssueTemplate(Issue.Level.Problem, "{0} {1} ms apart, should either be overlapped or at least {2} ms apart.", "timestamp - ", "gap", "threshold").WithCause("Two objects with a time gap less than 125 ms (240 bpm 1/2) are not overlapping.")
                },

                {
                    "Warning",
                    new IssueTemplate(Issue.Level.Warning, "{0} {1} ms apart.", "timestamp - ", "gap").WithCause("Two objects with a time gap less than 167 ms (180 bpm 1/2) are not overlapping.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var skipAfterDifficulty = Beatmap.Difficulty.Normal;

            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                // Beatmaps are sorted by interpreted difficulty.
                if (beatmap.GetDifficulty(true) > skipAfterDifficulty)
                    break;

                // This check only applies to easy/lowest diff normals, so if we find an easy, normals cannot not be lowest diff.
                if (beatmap.GetDifficulty(true) == Beatmap.Difficulty.Easy)
                    skipAfterDifficulty = Beatmap.Difficulty.Easy;

                foreach (var hitObject in beatmap.HitObjects)
                {
                    if (!(hitObject.Next() is HitObject nextObject))
                        continue;

                    // Slider ends do not need to overlap, same with spinners, spinners should be ignored overall.
                    if (!(hitObject is Circle) || nextObject is Spinner)
                        continue;

                    if (nextObject.time - hitObject.time >= WarningThreshold)
                        continue;

                    double distance = (nextObject.Position - hitObject.Position).Length();

                    // If the distance is larger or equal to the diameter of a circle, then they're not overlapping.
                    var radius = beatmap.DifficultySettings.GetCircleRadius();

                    if (distance < radius * 2)
                        continue;

                    if (nextObject.time - hitObject.time < ProblemThreshold)
                        yield return new Issue(GetTemplate("Problem"), beatmap, Timestamp.Get(hitObject, nextObject), $"{nextObject.time - hitObject.time:0.##}", ProblemThreshold).ForDifficulties(Beatmap.Difficulty.Easy, Beatmap.Difficulty.Normal);

                    else
                        yield return new Issue(GetTemplate("Warning"), beatmap, Timestamp.Get(hitObject, nextObject), $"{nextObject.time - hitObject.time:0.##}").ForDifficulties(Beatmap.Difficulty.Easy, Beatmap.Difficulty.Normal);
                }
            }
        }
    }
}