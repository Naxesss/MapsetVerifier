using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Standard.Compose
{
    [Check]
    public class CheckObscuredReverse : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes =
                [
                    Beatmap.Mode.Standard
                ],
                Difficulties =
                [
                    Beatmap.Difficulty.Easy,
                    Beatmap.Difficulty.Normal,
                    Beatmap.Difficulty.Hard,
                    Beatmap.Difficulty.Insane
                ],
                Category = "Compose",
                Message = "Obscured reverse arrows.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Preventing slider reverses from being covered up by other objects or combo bursts before players can react to them.

                        ![](https://i.imgur.com/BS8BkT7.png)
                        Although many skins remove combo bursts, these can hide reverses under them in the same way other objects can in gameplay, so only looking at the editor is a bit misleading."
                    },
                    {
                        "Reasoning",
                        @"
                        Some mappers like to stack objects on upcoming slider ends to make everything seem more coherent, but in doing so, reverses can become obscured and impossible to read unless you know they're there. For more experienced players, however, this isn't as much of a problem since you learn to hold sliders more efficiently and can react faster."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Obscured",
                    new IssueTemplate(Issue.Level.Warning, "{0} Reverse arrow {1} obscured.", "timestamp -", "(potentially)")
                        .WithCause("An object before a reverse arrow ends over where it appears close in time.")
                }
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            var closeThreshold = beatmap.DifficultySettings.GetCircleRadius() / 1.75;
            double tooCloseThreshold = beatmap.DifficultySettings.GetCircleRadius() / 3;

            // Represents the duration the reverse arrow is fully opaque.
            var opaqueTime = beatmap.DifficultySettings.GetPreemptTime();

            foreach (var hitObject in beatmap.HitObjects)
            {
                if (hitObject is not Slider slider || slider.EdgeAmount <= 1)
                    continue;

                var reverseTime = slider.time + slider.GetCurveDuration();
                var reversePosition = slider.GetPathPosition(reverseTime);

                var selectedObjects = new List<HitObject>();
                var isSerious = false;

                var hitObjectsRightBeforeReverse = beatmap.HitObjects.Where(otherHitObject => otherHitObject.GetEndTime() > reverseTime - opaqueTime && otherHitObject.GetEndTime() < reverseTime);

                foreach (var otherHitObject in hitObjectsRightBeforeReverse)
                {
                    // Spinners don't really obscure anything and are handled by recovery time anyway.
                    if (otherHitObject is Spinner)
                        continue;

                    float distanceToReverse;

                    if (otherHitObject is Slider otherSlider)
                        distanceToReverse = (float)Math.Sqrt(Math.Pow(otherSlider.EndPosition.X - reversePosition.X, 2) + Math.Pow(otherSlider.EndPosition.Y - reversePosition.Y, 2));
                    else
                        distanceToReverse = (float)Math.Sqrt(Math.Pow(otherHitObject.Position.X - reversePosition.X, 2) + Math.Pow(otherHitObject.Position.Y - reversePosition.Y, 2));

                    if (distanceToReverse < tooCloseThreshold)
                        isSerious = true;

                    if (distanceToReverse >= closeThreshold)
                        continue;

                    List<HitObject> hitObjects;

                    if (hitObject.time > otherHitObject.time)
                        hitObjects = [otherHitObject, hitObject];
                    else
                        hitObjects = [hitObject, otherHitObject];

                    selectedObjects.AddRange(hitObjects);

                    break;
                }

                if (selectedObjects.Count > 0)
                    yield return new Issue(GetTemplate("Obscured"), beatmap, Timestamp.Get(selectedObjects.ToArray()), isSerious ? "" : "potentially ");
            }
        }
    }
}