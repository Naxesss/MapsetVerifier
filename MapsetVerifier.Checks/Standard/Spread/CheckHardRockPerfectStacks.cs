using System.Numerics;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Settings;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Standard.Spread
{
    [Check]
    public class CheckHardRockPerfectStacks : BeatmapCheck
    {
        private sealed class StackState
        {
            public bool IsOnSlider { get; set; }
            public int StackIndex { get; set; }
        }

        private sealed class IssueCandidate
        {
            public required HitObject FirstHitObject { get; init; }
            public required HitObject SecondHitObject { get; init; }
            public required int DifficultyIndex { get; init; }
            public required bool IsPerfect { get; init; }
            public string? DistanceText { get; init; }
            public int? RequiredStackLeniency { get; init; }
            public string TemplateName => IsPerfect ? "Warning" : "Failed Stack";
        }

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = [Beatmap.Mode.Standard],
                Category = "Spread",
                Message = "Perfect stacks too close in time with Hard Rock applied.",
                Author = "Greaper",
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Failed Stack",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} Failed stack with Hard Rock applied, objects are {1} px apart, which is basically a perfect stack.",
                        "timestamp -",
                        "gap"
                    ).WithCause(
                        "Two objects are not perfectly overlapping, but with Hard Rock-adjusted CS they are within 1/14th of a circle radius of one another."
                    )
                },
                {
                    "Warning",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} Perfect stack with Hard Rock applied, stack leniency should be at least {1}.",
                        "timestamp -",
                        "stack leniency"
                    ).WithCause("Two objects overlap perfectly with Hard Rock-adjusted AR and CS.")
                },
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var stackables = beatmap.HitObjects.OfType<Stackable>().ToList();
            if (stackables.Count == 0)
                yield break;

            var states = ApplyHardRockStacking(beatmap, stackables);
            double[] snapping = [1, 1, 0.5, 0.25];

            var hrCircleSize = GetHardRockCircleSize(beatmap.DifficultySettings.circleSize);
            var hrCircleRadius = DifficultySettings.GetCircleRadius(hrCircleSize);
            var hrFadeInTime = GetHardRockFadeInTime(beatmap.DifficultySettings.approachRate);
            var candidates = new List<IssueCandidate>();
            var hitObjectCount = beatmap.HitObjects.Count;

            for (var i = 0; i < hitObjectCount - 1; ++i)
            for (var j = i + 1; j < hitObjectCount; ++j)
            {
                var hitObject = beatmap.HitObjects[i];
                var otherHitObject = beatmap.HitObjects[j];
                var timeDifference = otherHitObject.time - hitObject.time;

                if (hitObject is Spinner || otherHitObject is Spinner)
                    break;

                if (timeDifference >= snapping[0] * 60000 / 160d)
                    break;

                var difficultyIndex = GetHighestApplicableDifficultyIndex(timeDifference, snapping);
                if (difficultyIndex == null)
                    continue;

                var position = GetHardRockPosition(hitObject, states, hrCircleRadius);
                var otherPosition = GetHardRockPosition(otherHitObject, states, hrCircleRadius);

                if (position == otherPosition)
                {
                    candidates.Add(
                        new IssueCandidate
                        {
                            FirstHitObject = hitObject,
                            SecondHitObject = otherHitObject,
                            DifficultyIndex = difficultyIndex.Value,
                            IsPerfect = true,
                            RequiredStackLeniency = (int)
                                Math.Ceiling(timeDifference / (hrFadeInTime * 0.1)),
                        }
                    );

                    continue;
                }

                var distance = (position - otherPosition).Length();
                if (distance > hrCircleRadius / 14)
                    continue;

                candidates.Add(
                    new IssueCandidate
                    {
                        FirstHitObject = hitObject,
                        SecondHitObject = otherHitObject,
                        DifficultyIndex = difficultyIndex.Value,
                        IsPerfect = false,
                        DistanceText = $"{distance:0.##}",
                    }
                );
            }

            foreach (var group in GetIssueGroups(candidates))
            {
                var bestCandidate = group
                    .OrderByDescending(candidate => candidate.IsPerfect)
                    .ThenByDescending(candidate => candidate.DifficultyIndex)
                    .ThenByDescending(candidate => candidate.RequiredStackLeniency ?? int.MinValue)
                    .ThenBy(candidate => ParseDistance(candidate.DistanceText))
                    .First();

                var timestampHitObjects = group
                    .SelectMany(candidate =>
                        new[] { candidate.FirstHitObject, candidate.SecondHitObject }
                    )
                    .Distinct()
                    .OrderBy(hitObject => hitObject.time)
                    .ToArray();

                if (bestCandidate.IsPerfect)
                {
                    yield return new Issue(
                        GetTemplate(bestCandidate.TemplateName),
                        beatmap,
                        Timestamp.Get(timestampHitObjects),
                        bestCandidate.RequiredStackLeniency
                    );

                    continue;
                }

                yield return new Issue(
                    GetTemplate(bestCandidate.TemplateName),
                    beatmap,
                    Timestamp.Get(timestampHitObjects),
                    bestCandidate.DistanceText
                );
            }
        }

        private static int? GetHighestApplicableDifficultyIndex(
            double timeDifference,
            IReadOnlyList<double> snapping
        )
        {
            for (var diffIndex = snapping.Count - 1; diffIndex >= 0; --diffIndex)
            {
                var timeGap = snapping[diffIndex] * 60000 / 160d;
                if (timeDifference < timeGap)
                    return diffIndex;
            }

            return null;
        }

        private static float ParseDistance(string? distanceText) =>
            float.TryParse(distanceText, out var distance) ? distance : float.MaxValue;

        private static IEnumerable<List<IssueCandidate>> GetIssueGroups(
            IReadOnlyList<IssueCandidate> candidates
        )
        {
            var adjacency = new Dictionary<HitObject, List<IssueCandidate>>();

            foreach (var candidate in candidates)
            {
                adjacency.TryAdd(candidate.FirstHitObject, []);
                adjacency.TryAdd(candidate.SecondHitObject, []);
                adjacency[candidate.FirstHitObject].Add(candidate);
                adjacency[candidate.SecondHitObject].Add(candidate);
            }

            var visitedCandidates = new HashSet<IssueCandidate>();

            foreach (var candidate in candidates)
            {
                if (!visitedCandidates.Add(candidate))
                    continue;

                var group = new List<IssueCandidate>();
                var queue = new Queue<IssueCandidate>();
                queue.Enqueue(candidate);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    group.Add(current);

                    foreach (
                        var connected in adjacency[current.FirstHitObject]
                            .Concat(adjacency[current.SecondHitObject])
                    )
                        if (visitedCandidates.Add(connected))
                            queue.Enqueue(connected);
                }

                yield return group;
            }
        }

        private static Dictionary<Stackable, StackState> ApplyHardRockStacking(
            Beatmap beatmap,
            IReadOnlyList<Stackable> stackables
        )
        {
            var states = stackables.ToDictionary(stackable => stackable, _ => new StackState());
            var stackTimeThreshold = GetHardRockStackTimeThreshold(beatmap);

            bool wasChanged;
            do
            {
                wasChanged = false;

                for (var i = 0; i < stackables.Count - 1; ++i)
                for (var j = i + 1; j < stackables.Count; ++j)
                {
                    var hitObject = stackables[i];
                    var otherHitObject = stackables[j];

                    if (!MeetsStackTime(hitObject, otherHitObject, stackTimeThreshold))
                        break;

                    if (hitObject is Circle || otherHitObject is Circle)
                    {
                        if (ShouldStack(hitObject, otherHitObject, states, stackTimeThreshold))
                        {
                            if (otherHitObject is Slider || states[otherHitObject].IsOnSlider)
                                states[hitObject].IsOnSlider = true;

                            if (states[hitObject].StackIndex < 0 && !states[hitObject].IsOnSlider)
                            {
                                states[otherHitObject].StackIndex =
                                    states[hitObject].StackIndex - 1;
                                wasChanged = true;
                                break;
                            }

                            states[hitObject].StackIndex = states[otherHitObject].StackIndex + 1;
                            wasChanged = true;
                            break;
                        }

                        if (IsStacked(hitObject, otherHitObject, states, stackTimeThreshold))
                            break;
                    }

                    if (hitObject is not Slider slider)
                        continue;

                    if (ShouldStackTail(slider, otherHitObject, states, stackTimeThreshold))
                    {
                        if (otherHitObject is Slider || states[otherHitObject].IsOnSlider)
                        {
                            states[slider].IsOnSlider = true;
                            states[slider].StackIndex = states[otherHitObject].StackIndex + 1;
                        }
                        else
                        {
                            states[otherHitObject].StackIndex = states[slider].StackIndex - 1;
                        }

                        wasChanged = true;
                        break;
                    }

                    if (IsStackedTail(slider, otherHitObject, states, stackTimeThreshold))
                        break;
                }
            } while (wasChanged);

            return states;
        }

        private static bool MeetsStackTime(
            Stackable stackable,
            Stackable otherStackable,
            double stackTimeThreshold
        ) => otherStackable.time - stackable.time <= stackTimeThreshold;

        private static bool MeetsStackDistance(Stackable stackable, Stackable otherStackable) =>
            Vector2.DistanceSquared(stackable.UnstackedPosition, otherStackable.UnstackedPosition)
            < 3 * 3;

        private static bool CanStack(
            Stackable stackable,
            Stackable otherStackable,
            double stackTimeThreshold
        ) =>
            MeetsStackTime(stackable, otherStackable, stackTimeThreshold)
            && MeetsStackDistance(stackable, otherStackable);

        private static bool IsStacked(
            Stackable stackable,
            Stackable otherStackable,
            IReadOnlyDictionary<Stackable, StackState> states,
            double stackTimeThreshold
        ) =>
            CanStack(stackable, otherStackable, stackTimeThreshold)
            && states[stackable].StackIndex == states[otherStackable].StackIndex + 1;

        private static bool ShouldStack(
            Stackable stackable,
            Stackable otherStackable,
            IReadOnlyDictionary<Stackable, StackState> states,
            double stackTimeThreshold
        ) =>
            CanStack(stackable, otherStackable, stackTimeThreshold)
            && !IsStacked(stackable, otherStackable, states, stackTimeThreshold);

        private static bool CanStackTail(
            Slider slider,
            Stackable stackable,
            double stackTimeThreshold
        )
        {
            var sliderReferencePosition =
                slider.EdgeAmount % 2 == 0 ? slider.UnstackedPosition : slider.UnstackedEndPosition;
            return MeetsStackTime(slider, stackable, stackTimeThreshold)
                && Vector2.DistanceSquared(stackable.UnstackedPosition, sliderReferencePosition)
                    < 3 * 3
                && slider.time < stackable.time;
        }

        private static bool IsStackedTail(
            Slider slider,
            Stackable stackable,
            IReadOnlyDictionary<Stackable, StackState> states,
            double stackTimeThreshold
        ) =>
            CanStackTail(slider, stackable, stackTimeThreshold)
            && states[slider].StackIndex == states[stackable].StackIndex + 1;

        private static bool ShouldStackTail(
            Slider slider,
            Stackable stackable,
            IReadOnlyDictionary<Stackable, StackState> states,
            double stackTimeThreshold
        ) =>
            CanStackTail(slider, stackable, stackTimeThreshold)
            && !IsStackedTail(slider, stackable, states, stackTimeThreshold);

        private static Vector2 GetHardRockPosition(
            HitObject hitObject,
            IReadOnlyDictionary<Stackable, StackState> states,
            float circleRadius
        ) =>
            hitObject is Stackable stackable
                ? GetStackedPosition(
                    stackable.UnstackedPosition,
                    states[stackable].StackIndex,
                    circleRadius
                )
                : hitObject.Position;

        private static Vector2 GetStackedPosition(
            Vector2 position,
            int stackIndex,
            float circleRadius
        ) =>
            new(
                position.X + stackIndex * circleRadius * -0.1f,
                position.Y + stackIndex * circleRadius * -0.1f
            );

        private static double GetHardRockFadeInTime(float approachRate) =>
            DifficultySettings.DifficultyRange(
                GetHardRockApproachRate(approachRate),
                (450, 1200, 1800)
            );

        private static double GetHardRockStackTimeThreshold(Beatmap beatmap) =>
            GetHardRockFadeInTime(beatmap.DifficultySettings.approachRate)
            * beatmap.GeneralSettings.stackLeniency
            * 0.1;

        private static float GetHardRockApproachRate(float approachRate) =>
            Math.Min(10, approachRate * 1.4f);

        private static float GetHardRockCircleSize(float circleSize) =>
            Math.Min(10, circleSize * 1.3f);
    }
}
