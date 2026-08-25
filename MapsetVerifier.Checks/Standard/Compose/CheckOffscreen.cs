using System.Globalization;
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
    public class CheckOffscreen : BeatmapCheck
    {
        // Old measurements: -60, 430, -66, 578
        // New measurements: -60, 428, -67, 579 (tested with slider tails)
        private const int UPPER_LIMIT = -60;
        private const int LOWER_LIMIT = 428;
        private const int LEFT_LIMIT = -67;
        private const int RIGHT_LIMIT = 579;

        // The limits above are measured rather than exact, and the game additionally rounds positions when
        // rendering, so an object which is technically onscreen by a fraction of a pixel can still end up
        // partially offscreen in-game. Those cases are pointed out so they can be checked manually.
        private const float BorderlineMargin = 1;

        public override CheckMetadata GetMetadata() =>
            new BeatmapCheckMetadata
            {
                Modes = [Beatmap.Mode.Standard],
                Category = "Compose",
                Message = "Offscreen hit objects.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Preventing the border of hit objects from even partially becoming offscreen in 4:3 aspect ratios.

                        > 4:3 is included in 16:9 and 16:10, the only difference is the width, so you can check for offscreens along the top and bottom in any of these aspect ratios and it will look the same.

                        ![](assets/checks/standard-compose-offscreen-1.png ""A slider end which is partially offscreen along the bottom of the screen."")
                        "
                    },
                    {
                        "Reasoning",
                        @"
                        Although everything is technically readable and playable if an object is only partially offscreen, it trips up players using relative movement input (for example mouse) when their cursor hits the side of the screen, since the game will offset the cursor back into the screen which is difficult to correct while in the middle of gameplay.

                        Since objects partially offscreen also have a smaller area to hit, if not hitting the screen causing the problems above, it makes those objects need more precision to play which isn't consistent with how the rest of the game works, especially considering that the punishment for overshooting is getting your cursor offset slightly but still hitting the object and not missing like you probably would otherwise.

                        The screen limits used here are measured values rather than exact ones, and the game rounds object positions when rendering them, so objects ending up within a pixel of those limits are pointed out as something to verify manually rather than being ignored."
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Offscreen",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "{0} {1} is offscreen.",
                        "timestamp -",
                        "object"
                    ).WithCause(
                        "The border of a hit object is partially off the screen in 4:3 aspect ratios."
                    )
                },
                {
                    "Prevented",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} {1} would be offscreen, but the game prevents it.",
                        "timestamp -",
                        "object"
                    ).WithCause(
                        "The .osu code implies the hit object is in a place where it would be off the 512x512 playfield area, but the game has moved it back inside the screen automatically."
                    )
                },
                {
                    "Borderline",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} {1} is only {2} px away from being offscreen, ensure the entire white border is visible on a 4:3 aspect ratio.",
                        "timestamp -",
                        "object",
                        "amount"
                    ).WithCause(
                        "The border of a hit object is within a pixel of the screen edge in 4:3 aspect ratios, which due to the rounding the game applies can still end up offscreen."
                    )
                },
                {
                    "Bezier Margin",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "{0} Slider body is possibly offscreen, ensure the entire white border is visible on a 4:3 aspect ratio.",
                        "timestamp -"
                    ).WithCause(
                        "The slider body of a bezier slider is approximated to be 1 osu!pixel away from being offscreen at some point on its curve."
                    )
                },
            };

        public override IEnumerable<Issue> GetIssues(Beatmap beatmap)
        {
            foreach (var hitObject in beatmap.HitObjects)
            {
                var type = hitObject is Circle ? "Circle" : "Slider head";

                if (hitObject is not Circle && hitObject is not Slider)
                    continue;

                var borderlineReported = false;
                var circleRadius = beatmap.DifficultySettings.GetCircleRadius();
                var stackedOffset = new Vector2(0, 0);

                if (hitObject is Stackable stackable)
                    stackedOffset = stackable.Position - stackable.UnstackedPosition;

                if (hitObject.Position.Y + circleRadius > LOWER_LIMIT)
                {
                    yield return new Issue(
                        GetTemplate("Offscreen"),
                        beatmap,
                        Timestamp.Get(hitObject),
                        type
                    );
                }
                // The game prevents the head of objects from going offscreen inside a 512 by 512 px square,
                // meaning heads can still go offscreen at the bottom due to how aspect ratios work.
                else if (GetOffscreenBy(hitObject.Position, beatmap) > 0)
                {
                    // It does not prevent stacked objects from going offscreen, though.

                    // for each stackindex it goes 3px up and left, so for it to be prevented it'd be
                    // top, left : stackindex <= 0
                    // right     : stackindex >= 0

                    var stackableObject = (Stackable)hitObject;

                    var goesOffscreenTopOrLeft =
                        (
                            stackableObject.Position.Y - circleRadius < UPPER_LIMIT
                            || stackableObject.Position.X - circleRadius < LEFT_LIMIT
                        )
                        && stackableObject.stackIndex > 0;

                    var goesOffscreenRight =
                        stackableObject.Position.X + circleRadius > RIGHT_LIMIT
                        && stackableObject.stackIndex < 0;

                    if (goesOffscreenTopOrLeft || goesOffscreenRight)
                        yield return new Issue(
                            GetTemplate("Offscreen"),
                            beatmap,
                            Timestamp.Get(hitObject),
                            type
                        );
                    else
                        yield return new Issue(
                            GetTemplate("Prevented"),
                            beatmap,
                            Timestamp.Get(hitObject),
                            type
                        );
                }
                // Only the bottom is relevant here, as any other edge a head could come close to is one
                // the game moves it away from anyway.
                else if (hitObject.Position.Y + circleRadius > LOWER_LIMIT - BorderlineMargin)
                {
                    yield return new Issue(
                        GetTemplate("Borderline"),
                        beatmap,
                        Timestamp.Get(hitObject),
                        type,
                        FormatMargin(GetOffscreenAmount(hitObject.Position, beatmap))
                    );

                    borderlineReported = true;
                }

                if (hitObject is not Slider slider)
                    continue;

                if (GetOffscreenBy(slider.EndPosition, beatmap) > 0)
                {
                    yield return new Issue(
                        GetTemplate("Offscreen"),
                        beatmap,
                        Timestamp.Get(hitObject.GetEndTime()),
                        "Slider tail"
                    );

                    continue;
                }

                if (IsBorderline(slider.EndPosition, beatmap))
                {
                    yield return new Issue(
                        GetTemplate("Borderline"),
                        beatmap,
                        Timestamp.Get(hitObject.GetEndTime()),
                        "Slider tail",
                        FormatMargin(GetOffscreenAmount(slider.EndPosition, beatmap))
                    );

                    borderlineReported = true;
                }

                var bodyIssueFound = false;

                foreach (var pathPosition in slider.PathPxPositions)
                {
                    if (GetOffscreenBy(pathPosition + stackedOffset, beatmap) <= 0)
                        continue;

                    yield return new Issue(
                        GetTemplate("Offscreen"),
                        beatmap,
                        Timestamp.Get(hitObject),
                        "Slider body"
                    );

                    bodyIssueFound = true;

                    break;
                }

                if (bodyIssueFound)
                    continue;

                // Since we sample parts of slider bodies, and these aren't math formulas (although they could be),
                // we'd need to sample an infinite amount of points on the path, which is too intensive, so instead
                // we approximate and apply leniency to ensure false-positive over false-negative.
                var isNearEdge =
                    slider.CurveType != Slider.Curve.Linear
                    && slider.PathPxPositions.Any(pathPosition =>
                        GetOffscreenBy(pathPosition + stackedOffset, beatmap, 2) > 0
                    );

                if (isNearEdge)
                {
                    // The samples above are around a pixel apart, which is too coarse to tell a borderline
                    // body from a safe one, so the curve is walked in much smaller steps here.
                    var offscreenAmount = float.NegativeInfinity;

                    for (var j = 0; j < slider.GetCurveDuration() * 50; ++j)
                    {
                        var exactPathPosition =
                            slider.GetPathPosition(slider.time + j / 50d) + stackedOffset;

                        offscreenAmount = Math.Max(
                            offscreenAmount,
                            GetOffscreenAmount(exactPathPosition, beatmap)
                        );
                    }

                    if (offscreenAmount > 0)
                        yield return new Issue(
                            GetTemplate("Offscreen"),
                            beatmap,
                            Timestamp.Get(hitObject),
                            "Slider body"
                        );
                    // The head and tail are part of the body, so a borderline edge there already covers this.
                    else if (!borderlineReported)
                    {
                        if (offscreenAmount > -BorderlineMargin)
                            yield return new Issue(
                                GetTemplate("Borderline"),
                                beatmap,
                                Timestamp.Get(hitObject),
                                "Slider body",
                                FormatMargin(offscreenAmount)
                            );
                        else
                            yield return new Issue(
                                GetTemplate("Bezier Margin"),
                                beatmap,
                                Timestamp.Get(hitObject)
                            );
                    }

                    continue;
                }

                if (borderlineReported)
                    continue;

                var borderlinePosition = GetClosestBorderlinePosition(
                    slider,
                    stackedOffset,
                    beatmap
                );

                if (borderlinePosition != null)
                    yield return new Issue(
                        GetTemplate("Borderline"),
                        beatmap,
                        Timestamp.Get(hitObject),
                        "Slider body",
                        FormatMargin(GetOffscreenAmount(borderlinePosition.Value, beatmap))
                    );
            }
        }

        /// <summary> Returns the sampled path position closest to the screen edge, or null if none are borderline. </summary>
        private static Vector2? GetClosestBorderlinePosition(
            Slider slider,
            Vector2 stackedOffset,
            Beatmap beatmap
        )
        {
            Vector2? closestPosition = null;

            foreach (var pathPosition in slider.PathPxPositions)
            {
                var exactPathPosition = pathPosition + stackedOffset;

                if (!IsBorderline(exactPathPosition, beatmap))
                    continue;

                if (
                    closestPosition == null
                    || GetOffscreenAmount(exactPathPosition, beatmap)
                        > GetOffscreenAmount(closestPosition.Value, beatmap)
                )
                    closestPosition = exactPathPosition;
            }

            return closestPosition;
        }

        /// <summary>
        ///     Returns whether a point is onscreen, but by less than a pixel, in which case the rounding the
        ///     game applies can still end up pushing it offscreen.
        /// </summary>
        private static bool IsBorderline(Vector2 point, Beatmap beatmap) =>
            GetOffscreenBy(point, beatmap) <= 0
            && GetOffscreenAmount(point, beatmap) > -BorderlineMargin;

        /// <summary> Returns how many pixels are left before becoming offscreen, as a display string. </summary>
        private static string FormatMargin(float offscreenAmount)
        {
            var margin = Math.Max(-offscreenAmount, 0);

            return (Math.Floor(margin * 100) / 100).ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary> Returns how far offscreen an object is in pixels (in-game pixels, not resolution). </summary>
        private static float GetOffscreenBy(Vector2 point, Beatmap beatmap, float leniency = 0)
        {
            var offscreenBy = GetOffscreenAmount(point, beatmap) + leniency;

            if (offscreenBy < 0)
                offscreenBy = 0;

            return (float)Math.Ceiling(offscreenBy * 100) / 100f;
        }

        /// <summary>
        ///     Returns how far offscreen a point is in pixels (in-game pixels, not resolution), where negative
        ///     values represent how far it still is from the screen edge.
        /// </summary>
        private static float GetOffscreenAmount(Vector2 point, Beatmap beatmap)
        {
            var circleRadius = beatmap.DifficultySettings.GetCircleRadius();

            var offscreenRight = point.X + circleRadius - RIGHT_LIMIT;
            var offscreenLeft = circleRadius - point.X + LEFT_LIMIT;
            var offscreenLower = point.Y + circleRadius - LOWER_LIMIT;
            var offscreenUpper = circleRadius - point.Y + UPPER_LIMIT;

            return Math.Max(
                Math.Max(offscreenRight, offscreenLeft),
                Math.Max(offscreenLower, offscreenUpper)
            );
        }
    }
}
