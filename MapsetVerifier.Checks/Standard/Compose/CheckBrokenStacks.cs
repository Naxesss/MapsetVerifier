using System.Numerics;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Standard.Compose
{
    [Check]
    public class CheckBrokenStacks : BeatmapCheck
    {
        /// <summary> The game stacks objects whose heads are closer than this, see <see cref="Beatmap" />. </summary>
        private const float StackDistance = 3;

        /// <summary> Objects closer than this fraction of the circle radius read as an attempted stack. </summary>
        private const float BrokenStackRadiusFraction = 1 / 3f;

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = [Beatmap.Mode.Standard],
                Category = "Compose",
                Message = "Broken stacks.",
                Author = "Greaper",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Pointing out objects which are placed almost, but not quite, on top of each other, so the
                        game does not stack them."
                    },
                    {
                        "Reasoning",
                        @"
                        The game only stacks objects that are less than 3 pixels apart. Objects placed a few pixels
                        further apart than that are drawn nearly on top of each other without the usual stack offset,
                        which looks like a stack that went wrong. This usually comes from moving one object of a
                        stack slightly by accident, but can be intentional, so make sure it is."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Broken Stack",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} Broken stack, objects are {1} px apart, ensure this is intentional.",
                        "timestamp -",
                        "distance"
                    ).WithCause(
                        "Objects close in time are at least 3 px apart, so they do not stack, but are still "
                            + "within a third of a circle radius of each other."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var stackables = beatmap.HitObjects.OfType<Stackable>().ToList();
            var maxDistance =
                beatmap.DifficultySettings.GetCircleRadius() * BrokenStackRadiusFraction;
            var stackTime =
                beatmap.DifficultySettings.GetFadeInTime()
                * beatmap.GeneralSettings.stackLeniency
                * 0.1;

            // Runs of back-to-back objects that each sit on or near the previous one. A slider ends a run: what
            // follows it is placed relative to where it ends, which is not an object anyone sees stacked.
            var run = new List<Stackable>();
            var distances = new List<float>();

            foreach (var stackable in stackables)
            {
                if (run.Count > 0 && run[^1] is Circle previous)
                {
                    var distance = Vector2.Distance(
                        previous.UnstackedPosition,
                        stackable.UnstackedPosition
                    );

                    if (stackable.time - previous.time <= stackTime && distance < maxDistance)
                    {
                        run.Add(stackable);
                        distances.Add(distance);
                        continue;
                    }
                }

                if (GetIssue(beatmap, run, distances) is { } issue)
                    yield return issue;

                run = [stackable];
                distances = [];
            }

            if (GetIssue(beatmap, run, distances) is { } lastIssue)
                yield return lastIssue;
        }

        /// <summary>
        ///     How much longer the gap to the stacked object may be than the gap to the object that is off, or the
        ///     other way around. A stack is played in one rhythm; a stream that ends near an object several beats
        ///     later only happens to be placed close to it.
        /// </summary>
        private const double MaxRhythmRatio = 2.05;

        /// <summary>
        ///     An object only slightly off, right next to objects that do stack in the same rhythm, shows a stack
        ///     was meant. Two such objects in a row are not that: a stack moving a few pixels each object is a
        ///     pattern of its own.
        /// </summary>
        private Issue? GetIssue(Beatmap beatmap, List<Stackable> run, List<float> distances)
        {
            // distances[i] and Gap(i) are both about the step from run[i] to run[i + 1].
            double Gap(int i) => run[i + 1].time - run[i].time;
            bool IsStacked(int i) => i >= 0 && i < distances.Count && distances[i] < StackDistance;
            bool IsBroken(int i) => i >= 0 && i < distances.Count && distances[i] >= StackDistance;

            bool SameRhythm(int i, int j) =>
                Math.Max(Gap(i), Gap(j)) <= MaxRhythmRatio * Math.Max(Math.Min(Gap(i), Gap(j)), 1);

            bool StackedBeside(int i, int j) => IsStacked(j) && SameRhythm(i, j);

            var broken = Enumerable
                .Range(0, distances.Count)
                .Where(i =>
                    IsBroken(i)
                    && (StackedBeside(i, i - 1) || StackedBeside(i, i + 1))
                    && !IsBroken(i - 1)
                    && !IsBroken(i + 1)
                )
                .ToList();

            if (broken.Count == 0)
                return null;

            return new Issue(
                GetTemplate("Broken Stack"),
                beatmap,
                Timestamp.Get(run.Cast<HitObject>().ToArray()),
                $"{broken.Max(i => distances[i]):0.##}"
            );
        }
    }
}
