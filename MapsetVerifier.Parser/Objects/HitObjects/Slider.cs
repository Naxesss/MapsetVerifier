using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;
using MathNet.Numerics;

namespace MapsetVerifier.Parser.Objects.HitObjects
{
    public class Slider : Stackable
    {
        /// <summary> Determines how slider nodes affect the resulting shape of the slider. </summary>
        public enum Curve
        {
            Linear,
            Passthrough,
            Bezier,
            Catmull
        }
        // 319,179,1392,6,0,L|389:160,2,62.5,2|0|0,0:0|0:0|0:0,0:0:0:0:
        // x, y, time, typeFlags, hitsound, (sliderPath, edgeAmount, pixelLength, hitsoundEdges, additionEdges,) extras

        private const double STEP_LENGTH = 0.0005;

        public Curve CurveType { get; }
        public int EdgeAmount { get; }
        public HitSample.SamplesetType EndAddition { get; }

        public HitSounds EndHitSound { get; }
        public HitSample.SamplesetType EndSampleset { get; }

        public double EndTime { get; }
        public List<Vector2> NodePositions { get; }

        public List<Vector2> PathPxPositions { get; }
        public float PixelLength { get; }
        public List<Vector2> RedAnchorPositions { get; }
        public List<HitSample.SamplesetType> ReverseAdditions { get; }

        public List<HitSounds> ReverseHitSounds { get; }
        public List<HitSample.SamplesetType> ReverseSamplesets { get; }
        public List<double> SliderTickTimes { get; }
        public HitSample.SamplesetType StartAddition { get; }

        // Hit sounding
        public HitSounds StartHitSound { get; }
        public HitSample.SamplesetType StartSampleset { get; }

        // Cached values
        private List<Vector2>? CachedBezierPoints { get; set; }
        private double? CachedCurveDuration { get; set; }
        private double? CachedCurveLength { get; set; }
        private double? CachedSliderSpeed { get; set; }

        public Slider(string[] args, Beatmap beatmap) : base(args, beatmap)
        {
            CurveType = GetSliderType(args);
            NodePositions = GetNodes(args).ToList();
            EdgeAmount = GetEdgeAmount(args);
            PixelLength = GetPixelLength(args);

            // Hit sounding
            var edgeHitSounds = GetEdgeHitSounds(args);
            var edgeAdditions = GetEdgeAdditions(args);

            StartHitSound = edgeHitSounds.Item1;
            StartSampleset = edgeAdditions.Item1;
            StartAddition = edgeAdditions.Item2;

            EndHitSound = edgeHitSounds.Item2;
            EndSampleset = edgeAdditions.Item3;
            EndAddition = edgeAdditions.Item4;

            ReverseHitSounds = edgeHitSounds.Item3.ToList();
            ReverseSamplesets = edgeAdditions.Item5.ToList();
            ReverseAdditions = edgeAdditions.Item6.ToList();

            // Non-explicit
            if (beatmap != null)
            {
                RedAnchorPositions = GetRedAnchors().ToList();
                PathPxPositions = GetPathPxPositions();
                EndTime = GetEndTime();
                SliderTickTimes = GetSliderTickTimes();

                UnstackedEndPosition = EdgeAmount % 2 == 1 ? PathPxPositions.Last() : UnstackedPosition;

                // Difficulty
                LazyEndPosition = Position;
                LazyTravelDistance = 0;
            }
            else
            {
                RedAnchorPositions = [];
                PathPxPositions = [];
                EndTime = 0;
                SliderTickTimes = [];
            }

            usedHitSamples = GetUsedHitSamples().ToList();
        }

        public sealed override Vector2 Position => base.Position;

        public Vector2 UnstackedEndPosition { get; }
        public Vector2 EndPosition => UnstackedEndPosition + Position - UnstackedPosition;

        public Vector2? LazyEndPosition { get; set; }
        public double LazyTravelDistance { get; set; }

        /*
         *  Parsing
         */

        private static Curve GetSliderType(IReadOnlyList<string> args)
        {
            var type = args[5].Split('|')[0];

            return type == "L" ? Curve.Linear :
                type == "P" ? Curve.Passthrough :
                type == "B" ? Curve.Bezier : Curve.Catmull; // Catmull is the default curve type.
        }

        private IEnumerable<Vector2> GetNodes(IReadOnlyList<string> args)
        {
            // The first position is also a node in the editor, so we count that too.
            yield return Position;

            var sliderPath = args[5];

            foreach (var node in sliderPath.Split('|'))
                // Parses node format (e.g. P|128:50|172:291).
                if (node.Length > 1)
                {
                    var x = float.Parse(node.Split(':')[0]);
                    var y = float.Parse(node.Split(':')[1]);

                    yield return new Vector2(x, y);
                }
        }

        private static int GetEdgeAmount(IReadOnlyList<string> args) => int.Parse(args[6]);

        private static float GetPixelLength(IReadOnlyList<string> args) => float.Parse(args[7], CultureInfo.InvariantCulture);

        private Tuple<HitSounds, HitSounds, List<HitSounds>> GetEdgeHitSounds(IReadOnlyList<string> args)
        {
            HitSounds edgeStartHitSound = 0;
            HitSounds edgeEndHitSound = 0;
            var edgeReverseHitSounds = new List<HitSounds>();

            if (args.Count > 8)
            {
                // Not set in some situations (e.g. older file versions or no hit sounds).
                var edgeHitSoundStr = args[8];

                if (!edgeHitSoundStr.Contains("|"))
                    return Tuple.Create(edgeStartHitSound, edgeEndHitSound, edgeReverseHitSounds);

                for (var i = 0; i < edgeHitSoundStr.Split('|').Length; ++i)
                {
                    var edgeHitSound = (HitSounds)int.Parse(edgeHitSoundStr.Split('|')[i]);

                    if (i == 0)
                        edgeStartHitSound = edgeHitSound;
                    else if (i == edgeHitSoundStr.Split('|').Length - 1)
                        edgeEndHitSound = edgeHitSound;
                    else
                        // Any not first or last are for the reverses.
                        edgeReverseHitSounds.Add(edgeHitSound);
                }
            }
            else
            {
                // If an object has no complex hit sounding, it omits fields such as edge
                // hit sounds. Instead, it simply uses one hit sound over everything.
                edgeStartHitSound = hitSound;
                edgeEndHitSound = hitSound;

                for (var i = 0; i < EdgeAmount; ++i)
                    edgeReverseHitSounds.Add(hitSound);
            }

            return Tuple.Create(edgeStartHitSound, edgeEndHitSound, edgeReverseHitSounds);
        }

        private Tuple<HitSample.SamplesetType, HitSample.SamplesetType, HitSample.SamplesetType, HitSample.SamplesetType, List<HitSample.SamplesetType>, List<HitSample.SamplesetType>> GetEdgeAdditions(string[] args)
        {
            HitSample.SamplesetType edgeStartSampleset = 0;
            HitSample.SamplesetType edgeStartAddition = 0;

            HitSample.SamplesetType edgeEndSampleset = 0;
            HitSample.SamplesetType edgeEndAddition = 0;

            var edgeReverseSamplesets = new List<HitSample.SamplesetType>();
            var edgeReverseAdditions = new List<HitSample.SamplesetType>();

            if (args.Count() > 9)
            {
                // Not set in some situations (e.g. older file versions or no hit sounds).
                var edgeAdditions = args[9];

                if (edgeAdditions.Contains("|"))
                    for (var i = 0; i < edgeAdditions.Split('|').Length; ++i)
                    {
                        var edgeSampleset = (HitSample.SamplesetType)int.Parse(edgeAdditions.Split('|')[i].Split(':')[0]);
                        var edgeAddition = (HitSample.SamplesetType)int.Parse(edgeAdditions.Split('|')[i].Split(':')[1]);

                        if (i == 0)
                        {
                            edgeStartSampleset = edgeSampleset;
                            edgeStartAddition = edgeAddition;
                        }
                        else if (i == edgeAdditions.Split('|').Length - 1)
                        {
                            edgeEndSampleset = edgeSampleset;
                            edgeEndAddition = edgeAddition;
                        }
                        else
                        {
                            edgeReverseSamplesets.Add(edgeSampleset);
                            edgeReverseAdditions.Add(edgeAddition);
                        }
                    }
            }
            else
            {
                // If an object has no complex hit sounding, it omits fields such as edge
                // hit sounds. Instead, it simply uses one hit sound over everything.
                edgeStartSampleset = sampleset;
                edgeEndSampleset = sampleset;

                for (var i = 0; i < EdgeAmount; ++i)
                    edgeReverseSamplesets.Add(sampleset);

                edgeStartAddition = addition;
                edgeEndAddition = addition;

                for (var i = 0; i < EdgeAmount; ++i)
                    edgeReverseAdditions.Add(addition);
            }

            return Tuple.Create(edgeStartSampleset, edgeStartAddition, edgeEndSampleset, edgeEndAddition, edgeReverseSamplesets, edgeReverseAdditions);
        }

        /*
         *  Non-Explicit
         */

        private new double GetEndTime()
        {
            var start = time;
            var curveDuration = GetCurveDuration();
            var exactEndTime = start + curveDuration * EdgeAmount;

            return exactEndTime + beatmap.GetPracticalUnsnap(exactEndTime);
        }

        private IEnumerable<Vector2> GetRedAnchors()
        {
            if (NodePositions.Count > 0)
            {
                var prevPosition = NodePositions[0];

                for (var i = 1; i < NodePositions.Count; ++i)
                {
                    if (NodePositions[i] == prevPosition)
                        yield return NodePositions[i];

                    prevPosition = NodePositions[i];
                }
            }
        }

        private List<Vector2> GetPathPxPositions()
        {
            // Increase this to improve performance but lower accuracy.
            const double STEP_SIZE = 1;

            // First we need to get how fast the slider moves,
            var pxPerMs = GetSliderSpeed(time);

            // and then calculate this in steps accordingly.
            var currentPosition = UnstackedPosition;

            // Always start with the current position, means reverse sliders' end position is more accurate.
            var positions = new List<Vector2> { currentPosition };

            var limit = pxPerMs * GetCurveDuration() / STEP_SIZE;

            for (var i = 0; i < limit; ++i)
            {
                var prevPosition = currentPosition;
                var stepTime = time + i / pxPerMs * STEP_SIZE;

                currentPosition = GetPathPosition(stepTime);

                // Only add the position if it's different from the previous.
                if (currentPosition != prevPosition)
                    positions.Add(currentPosition);
            }

            var endPosition = GetPathPosition(time + limit / pxPerMs * STEP_SIZE);
            positions.Add(endPosition);

            return positions;
        }

        /*
         *  Utility
         */

        /// <summary> Returns the position on the curve at a given point in time (intensive, consider using mPathPxPositions). </summary>
        public Vector2 GetPathPosition(double time) =>
            CurveType switch
            {
                Curve.Linear => GetLinearPathPosition(time),
                Curve.Passthrough => GetPassthroughPathPosition(time),
                Curve.Bezier => GetBezierPathPosition(time),
                Curve.Catmull => GetCatmullPathPosition(time),
                _ => new Vector2(0, 0)
            };

        /// <summary> Returns the speed of any slider starting from the given time in px/ms. Caps SV within range 0.1-10. </summary>
        public double GetSliderSpeed(double time)
        {
            if (CachedSliderSpeed != null)
                return CachedSliderSpeed.GetValueOrDefault();

            var timingLine = beatmap.GetTimingLine<UninheritedLine>(time);
            if (timingLine == null)
            {
                throw new Exception($"No uninherited timing line found at time {this.time} for slider starting at {this.time}." );
            }

            var sliderStartTimingLine = beatmap.GetTimingLine(this.time);
            
            var msPerBeat = timingLine.msPerBeat;
            double effectiveSVMult = sliderStartTimingLine?.SvMult ?? 1.0;

            CachedSliderSpeed = 100 * effectiveSVMult * beatmap.DifficultySettings.sliderMultiplier / msPerBeat;

            return CachedSliderSpeed.GetValueOrDefault();
        }

        /// <summary> Returns the duration of the curve (i.e. from edge to edge), ignoring reverses. </summary>
        public double GetCurveDuration()
        {
            if (CachedCurveDuration != null)
                return CachedCurveDuration.GetValueOrDefault();

            CachedCurveDuration = PixelLength / GetSliderSpeed(time);

            return CachedCurveDuration.GetValueOrDefault();
        }

        /// <summary> Returns the sampleset on the head of the slider, optionally prioritizing the addition. </summary>
        public new HitSample.SamplesetType GetStartSampleset(bool additionOverrides = false)
        {
            if (additionOverrides && StartAddition != HitSample.SamplesetType.Auto)
                return StartAddition;

            // Inherits from timing line if auto.
            if (StartSampleset == HitSample.SamplesetType.Auto)
                return beatmap.GetTimingLine(time, true)?.Sampleset ?? HitSample.SamplesetType.Auto;

            return StartSampleset;
        }

        /// <summary> Returns the sampleset at a given reverse (starting from 0), optionally prioritizing the addition. </summary>
        public HitSample.SamplesetType GetReverseSampleset(int reverseIndex, bool additionOverrides = false)
        {
            var theoreticalStart = this.time - beatmap.GetTheoreticalUnsnap(this.time);
            double time = Timestamp.Round(theoreticalStart + GetCurveDuration() * (reverseIndex + 1));

            // Reverse additions and samplesets do not exist in file version 7 and below, hence ElementAtOrDefault.
            if (additionOverrides && ReverseAdditions.ElementAtOrDefault(reverseIndex) != HitSample.SamplesetType.Auto)
                return ReverseAdditions.ElementAt(reverseIndex);

            if (ReverseSamplesets.ElementAtOrDefault(reverseIndex) == HitSample.SamplesetType.Auto)
                return beatmap.GetTimingLine(time, true)?.Sampleset ?? HitSample.SamplesetType.Auto;

            return ReverseSamplesets.ElementAt(reverseIndex);
        }

        /// <summary> Returns the sampleset on the tail of the slider, optionally prioritizing the addition. </summary>
        public new HitSample.SamplesetType GetEndSampleset(bool additionOverrides = false)
        {
            if (additionOverrides && EndAddition != HitSample.SamplesetType.Auto)
                return EndAddition;

            if (EndSampleset == HitSample.SamplesetType.Auto)
                return beatmap.GetTimingLine(EndTime, true)?.Sampleset ?? HitSample.SamplesetType.Auto;

            return EndSampleset;
        }

        /// <summary> Returns how far along the curve a given point of time is (from 0 to 1), accounting for reverses. </summary>
        public double GetCurveFraction(double time)
        {
            var division = (time - this.time) / GetCurveDuration();
            var fraction = division - Math.Floor(division);

            if ((Math.Floor(division) % 2).AlmostEqual(1))
                fraction = 1 - fraction;

            return fraction;
        }

        /// <summary> Returns the length of the curve in px. </summary>
        public double GetCurveLength()
        {
            if (CachedCurveLength != null)
                return CachedCurveLength.GetValueOrDefault();

            CachedCurveLength = GetCurveDuration() * GetSliderSpeed(time);

            return CachedCurveLength.GetValueOrDefault();
        }

        /// <summary> Returns the points in time for all ticks of the slider, with decimal accuracy. </summary>
        public List<double> GetSliderTickTimes()
        {
            var timingLine = beatmap.GetTimingLine<UninheritedLine>(time);
            if (timingLine == null)
            {
                throw new Exception($"No uninherited timing line found at time for slider starting at {time}." );
            }
            
            var tickRate = beatmap.DifficultySettings.sliderTickRate;
            var msPerBeat = timingLine.msPerBeat;

            // Not entierly sure if it's based on theoretical time and cast to int or something else.
            // It doesn't seem to be practical time and then rounded to closest at least.
            var theoreticalTime = time - beatmap.GetTheoreticalUnsnap(time);

            // Only duration during which ticks can be present (so not the same ms as the tail).
            var duration = EndTime - time - 1;
            var ticks = (int)(duration / msPerBeat * tickRate);
            var tickTimes = new List<double>();

            for (var i = 1; i <= ticks; ++i)
                tickTimes.Add(Timestamp.Round(i * msPerBeat / tickRate + theoreticalTime));

            return tickTimes;
        }

        /*
         *  Mathematics
         */

        private Vector2 GetBezierPoint(List<Vector2> points, double fraction)
        {
            // See https://en.wikipedia.org/wiki/B%C3%A9zier_curve.
            // Finds the middle of middles at x, which is a variable between 0 and 1.
            // Note that this is not a constant movement, though.

            // Make sure to copy, don't reference; newPoints will be mutated.
            var newPoints = new List<Vector2>(points);

            var index = newPoints.Count - 1;

            while (index > 0)
            {
                for (var i = 0; i < index; i++)
                    newPoints[i] = newPoints[i] + (float)fraction * (newPoints[i + 1] - newPoints[i]);

                index--;
            }

            return newPoints[0];
        }

        private Vector2 GetCatmullPoint(Vector2 point0, Vector2 point1, Vector2 point2, Vector2 point3, double x)
        {
            // See https://en.wikipedia.org/wiki/Centripetal_Catmull%E2%80%93Rom_spline.

            var point = new Vector2();

            var x2 = (float)(x * x);
            var x3 = x2 * (float)x;

            point.X = 0.5f * (2.0f * point1.X + (-point0.X + point2.X) * (float)x + (2.0f * point0.X - 5.0f * point1.X + 4 * point2.X - point3.X) * x2 + (-point0.X + 3.0f * point1.X - 3.0f * point2.X + point3.X) * x3);

            point.Y = 0.5f * (2.0f * point1.Y + (-point0.Y + point2.Y) * (float)x + (2.0f * point0.Y - 5.0f * point1.Y + 4 * point2.Y - point3.Y) * x2 + (-point0.Y + 3.0f * point1.Y - 3.0f * point2.Y + point3.Y) * x3);

            return point;
        }

        private double GetDistance(Vector2 position, Vector2 otherPosition) =>
            Math.Sqrt(Math.Pow(position.X - otherPosition.X, 2) + Math.Pow(position.Y - otherPosition.Y, 2));

        /*
         *  Slider Pathing
         */

        private Vector2 GetLinearPathPosition(double time)
        {
            var fraction = GetCurveFraction(time);

            var pathLengths = new List<double>();
            var previousPosition = Position;

            for (var i = 1; i < NodePositions.Count; ++i)
            {
                // Since every node is interpreted as an anchor, we only need to worry about the last node.
                // Rest will be perfectly followed by just going straight to the node,
                double distance;

                if (i < NodePositions.Count - 1)
                {
                    distance = GetDistance(NodePositions.ElementAt(i), previousPosition);
                    previousPosition = NodePositions.ElementAt(i);
                }
                else
                    // but if it is the last node, then we need to look at the total length
                    // to see how far it goes in that direction.
                {
                    distance = GetCurveLength() - pathLengths.Sum();
                }

                pathLengths.Add(distance);
            }

            var fractionDistance = pathLengths.Sum() * fraction;
            var prevNodeIndex = 0;

            foreach (var pathLength in pathLengths)
            {
                ++prevNodeIndex;

                if (fractionDistance > pathLength)
                    fractionDistance -= pathLength;
                else
                    break;
            }

            if (prevNodeIndex >= NodePositions.Count())
                prevNodeIndex = NodePositions.Count() - 1;

            var startPoint = NodePositions.ElementAt(prevNodeIndex <= 0 ? 0 : prevNodeIndex - 1);
            var endPoint = NodePositions.ElementAt(prevNodeIndex);
            var pointDistance = GetDistance(startPoint, endPoint);
            var microFraction = fractionDistance / pointDistance;

            return startPoint + new Vector2((endPoint - startPoint).X * (float)microFraction, (endPoint - startPoint).Y * (float)microFraction);
        }

        private Vector2 GetPassthroughPathPosition(double time)
        {
            // Less than 3 interprets as linear.
            if (NodePositions.Count < 3)
                return GetLinearPathPosition(time);

            // More than 3 interprets as bezier.
            if (NodePositions.Count > 3)
                return GetBezierPathPosition(time);

            var secondPoint = NodePositions.ElementAt(1);
            var thirdPoint = NodePositions.ElementAt(2);

            // Center and radius of the circle.
            double divisor = 2 * (UnstackedPosition.X * (secondPoint.Y - thirdPoint.Y) + secondPoint.X * (thirdPoint.Y - UnstackedPosition.Y) + thirdPoint.X * (UnstackedPosition.Y - secondPoint.Y));

            if (divisor == 0)
                // Second point is somewhere straight between the first and third, making our path linear.
                return GetLinearPathPosition(time);

            var centerX = ((UnstackedPosition.X * UnstackedPosition.X + UnstackedPosition.Y * UnstackedPosition.Y) * (secondPoint.Y - thirdPoint.Y) + (secondPoint.X * secondPoint.X + secondPoint.Y * secondPoint.Y) * (thirdPoint.Y - UnstackedPosition.Y) + (thirdPoint.X * thirdPoint.X + thirdPoint.Y * thirdPoint.Y) * (UnstackedPosition.Y - secondPoint.Y)) / divisor;

            var centerY = ((UnstackedPosition.X * UnstackedPosition.X + UnstackedPosition.Y * UnstackedPosition.Y) * (thirdPoint.X - secondPoint.X) + (secondPoint.X * secondPoint.X + secondPoint.Y * secondPoint.Y) * (UnstackedPosition.X - thirdPoint.X) + (thirdPoint.X * thirdPoint.X + thirdPoint.Y * thirdPoint.Y) * (secondPoint.X - UnstackedPosition.X)) / divisor;

            var radius = Math.Sqrt(Math.Pow(centerX - UnstackedPosition.X, 2) + Math.Pow(centerY - UnstackedPosition.Y, 2));

            var radians = GetCurveLength() / radius;

            // Which direction to rotate based on which side the center is on.
            if ((secondPoint.X - UnstackedPosition.X) * (thirdPoint.Y - UnstackedPosition.Y) - (secondPoint.Y - UnstackedPosition.Y) * (thirdPoint.X - UnstackedPosition.X) < 0)
                radians *= -1.0f;

            // Getting the point on the circumference of the circle.
            var fraction = GetCurveFraction(time);

            var radianX = Math.Cos(fraction * radians);
            var radianY = Math.Sin(fraction * radians);

            var x = radianX * (UnstackedPosition.X - centerX) - radianY * (UnstackedPosition.Y - centerY) + centerX;
            var y = radianY * (UnstackedPosition.X - centerX) + radianX * (UnstackedPosition.Y - centerY) + centerY;

            return new Vector2((float)x, (float)y);
        }

        private List<Vector2> GetBezierPoints()
        {
            // Include the first point in the total slider points.
            var sliderPoints = NodePositions.ToList();

            var currentPoint = Position;
            var tempBezierPoints = new List<Vector2> { currentPoint };

            // For each anchor, calculate the curve, until we find where we need to be.
            var tteration = 0;

            var pixelsPerMs = GetSliderSpeed(time);
            double totalLength = 0;
            var fullLength = GetCurveDuration() * pixelsPerMs;

            while (tteration < sliderPoints.Count)
            {
                // Get all the nodes from one anchor/start point to the next.
                var points = new List<Vector2>();
                var currentIteration = tteration;

                for (var i = currentIteration; i < sliderPoints.Count; ++i)
                {
                    if (i > currentIteration && sliderPoints.ElementAt(i - 1) == sliderPoints.ElementAt(i))
                        break;

                    points.Add(sliderPoints.ElementAt(i));
                    ++tteration;
                }

                // Calculate how long this curve (not the whole thing, just from anchor to anchor) will be.
                var prevPoint = points.First();
                double curvePixelLength = 0;

                for (double k = 0.0f; k < 1.0f + STEP_LENGTH; k += STEP_LENGTH)
                    if (totalLength <= fullLength)
                    {
                        currentPoint = GetBezierPoint(points, k);
                        curvePixelLength += GetDistance(prevPoint, currentPoint);
                        prevPoint = currentPoint;

                        if (curvePixelLength >= pixelsPerMs * 2)
                        {
                            totalLength += curvePixelLength;
                            curvePixelLength = 0;
                            tempBezierPoints.Add(currentPoint);
                        }
                    }

                // As long as we haven't reached the last path between anchors, keep track of the length of the path.
                // Ensures that we can switch from one anchor path to another.
                if (tteration <= sliderPoints.Count)
                    totalLength += curvePixelLength;
                else
                    tempBezierPoints.Add(currentPoint);
            }

            return tempBezierPoints;
        }

        private Vector2 GetBezierPathPosition(double time)
        {
            CachedBezierPoints ??= GetBezierPoints();

            var fraction = GetCurveFraction(time);

            var integer = (int)Math.Floor(CachedBezierPoints.Count * fraction);
            var @decimal = (float)(CachedBezierPoints.Count * fraction - integer);

            return integer >= CachedBezierPoints.Count - 1 ? CachedBezierPoints[^1] : CachedBezierPoints[integer] + (CachedBezierPoints[integer + 1] - CachedBezierPoints[integer]) * @decimal;
        }

        private Vector2 GetCatmullPathPosition(double time)
        {
            // Any less than 3 points might as well be linear.
            if (NodePositions.Count < 3)
                return GetLinearPathPosition(time);

            var fraction = GetCurveFraction(time);

            var pixelsPerMs = GetSliderSpeed(this.time);
            double totalLength = 0;
            var desiredLength = GetCurveDuration() * pixelsPerMs * fraction;

            var points = new List<Vector2>(NodePositions);

            // Go through the curve until the fraction is reached.
            var prevPoint = points.First();

            for (var i = 0; i < points.Count - 1; ++i)
            {
                // Get the curve length between anchors.
                double curvePixelLength = 0;
                var prevCurvePoint = points[i];

                for (double k = 0.0f; k < 1.0f + STEP_LENGTH; k += STEP_LENGTH)
                {
                    Vector2 currentPoint;

                    if (i == 0)
                        // Double the start position.
                        currentPoint = GetCatmullPoint(points[i], points[i], points[i + 1], points[i + 2], k);
                    else if (i < points.Count - 2)
                        currentPoint = GetCatmullPoint(points[i - 1], points[i], points[i + 1], points[i + 2], k);
                    else
                        // Double the end position.
                        currentPoint = GetCatmullPoint(points[i - 1], points[i], points[i + 1], points[i + 1], k);

                    curvePixelLength += Math.Sqrt(Math.Pow(prevCurvePoint.X - currentPoint.X, 2) + Math.Pow(prevCurvePoint.Y - currentPoint.Y, 2));

                    prevCurvePoint = currentPoint;
                }

                double variable = 0;
                double curveLength = 0;

                while (true)
                {
                    Vector2 currentPoint;

                    if (i == 0)
                        currentPoint = GetCatmullPoint(points[i], points[i], points[i + 1], points[i + 2], variable);
                    else if (i < points.Count - 2)
                        currentPoint = GetCatmullPoint(points[i - 1], points[i], points[i + 1], points[i + 2], variable);
                    else
                        currentPoint = GetCatmullPoint(points[i - 1], points[i], points[i + 1], points[i + 1], variable);

                    curveLength += Math.Sqrt(Math.Pow(prevPoint.X - currentPoint.X, 2) + Math.Pow(prevPoint.Y - currentPoint.Y, 2));

                    if (totalLength + curveLength >= desiredLength)
                        return currentPoint;

                    prevPoint = currentPoint;

                    // Keeping track of the length of the path ensures that we can switch from one anchor path to another.
                    if (curveLength > curvePixelLength && i < points.Count - 2)
                    {
                        totalLength += curveLength;

                        break;
                    }

                    variable += STEP_LENGTH;
                }
            }

            return new Vector2(0, 0);
        }
    }
}