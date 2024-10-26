using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;
using MathNet.Numerics;

namespace MapsetVerifier.Parser.Objects
{
    public class HitObject
    {
        /// <summary> Determines which sounds will be played as feedback (can be combined, bitflags). </summary>
        [Flags]
        public enum HitSounds
        {
            None = 0,
            Normal = 1,
            Whistle = 2,
            Finish = 4,
            Clap = 8
        }

        /// <summary> Determines the properties of the hit object (can be combined, bitflags). </summary>
        [Flags]
        public enum Types
        {
            Circle = 1,
            Slider = 2,
            NewCombo = 4,
            Spinner = 8,
            ComboSkip1 = 16,
            ComboSkip2 = 32,
            ComboSkip3 = 64,
            ManiaHoldNote = 128
        }
        
        // 131,304,1166,1,0,0:0:0:0:                                        circle
        // 319,179,1392,6,0,L|389:160,2,62.5,2|0|0,0:0|0:0|0:0,0:0:0:0:     slider
        // 256,192,187300,12,0,188889,1:0:0:0:                              spinner

        // x, y, time, typeFlags, hitsound, extras                                                                               circle
        // x, y, time, typeFlags, hitsound, (sliderPath, edgeAmount, pixelLength, hitsoundEdges, additionEdges,) extras          slider
        // x, y, time, typeFlags, hitsound, (endTime,) extras                                                                    spinner

        public readonly Beatmap beatmap;
        public readonly string code;
        private int hitObjectIndex;
        
        public readonly double time;
        public readonly Types type;
        public readonly HitSounds hitSound;

        // extras
        // not all file versions have these, so they need to be nullable
        public readonly HitSample.SamplesetType addition;
        public readonly HitSample.SamplesetType sampleset;
        public readonly int? customIndex;
        public readonly int? volume;
        public readonly string filename;

        public List<HitSample> usedHitSamples = new();

        public HitObject(string[] args, Beatmap beatmap)
        {
            this.beatmap = beatmap;
            code = string.Join(",", args);

            Position = GetPosition(args);

            time = GetTime(args);
            type = GetTypeFlags(args);
            hitSound = GetHitSound(args);

            // extras
            var extras = GetExtras(args);

            if (extras != null)
            {
                // custom index and volume are by default 0 if there are edge hitsounds or similar
                sampleset = extras.Item1;
                addition = extras.Item2;
                customIndex = extras.Item3 == 0 ? null : extras.Item3;
                volume = extras.Item4 == 0 ? null : extras.Item4;

                // hitsound filenames only apply to circles and hold notes
                var hitSoundFile = extras.Item5;

                if (hitSoundFile.Trim() != "" && (HasType(Types.Circle) || HasType(Types.ManiaHoldNote)))
                    filename = PathStatic.ParsePath(hitSoundFile, false, true);
            }

            // Sliders and spinners include additional edges which support hit sounding, so we
            // should handle that after those edges are initialized in Slider/Spinner instead.
            if (!(this is Slider) && !(this is Spinner))
                usedHitSamples = GetUsedHitSamples().ToList();
        }

        public virtual Vector2 Position { get; }

        /*
         *  Parsing
         */

        private Vector2 GetPosition(string[] args)
        {
            var x = float.Parse(args[0], CultureInfo.InvariantCulture);
            var y = float.Parse(args[1], CultureInfo.InvariantCulture);

            return new Vector2(x, y);
        }

        private double GetTime(string[] args) => double.Parse(args[2], CultureInfo.InvariantCulture);

        private Types GetTypeFlags(string[] args) => (Types)int.Parse(args[3]);

        private HitSounds GetHitSound(string[] args) => (HitSounds)int.Parse(args[4]);

        private Tuple<HitSample.SamplesetType, HitSample.SamplesetType, int?, int?, string> GetExtras(string[] args)
        {
            var extras = args.Last();

            // Hold notes have "endTime:extras" as format.
            var index = HasType(Types.ManiaHoldNote) ? 1 : 0;

            if (extras.Contains(":"))
            {
                var samplesetValue = (HitSample.SamplesetType)int.Parse(extras.Split(':')[index]);
                var additionsValue = (HitSample.SamplesetType)int.Parse(extras.Split(':')[index + 1]);
                int? customIndexValue = int.Parse(extras.Split(':')[index + 2]);

                // Does not exist in file v11.
                int? volumeValue = null;

                if (extras.Split(':').Count() > index + 3)
                    volumeValue = int.Parse(extras.Split(':')[index + 3]);

                var filenameValue = "";

                if (extras.Split(':').Count() > index + 4)
                    filenameValue = extras.Split(':')[index + 4];

                return Tuple.Create(samplesetValue, additionsValue, customIndexValue, volumeValue, filenameValue);
            }

            return null;
        }

        /*
         *  Next / Prev
         */

        /// <summary> Returns the index of this hit object in the beatmap's hit object list, O(1). </summary>
        public int GetHitObjectIndex() => hitObjectIndex;

        /// <summary>
        ///     Sets the index of this hit object. This should reflect the index in the hit object list of the beatmap.
        ///     Only use this if you're changing the order of objects or adding new ones after parsing.
        /// </summary>
        public void SetHitObjectIndex(int index) => hitObjectIndex = index;

        /// <summary>
        ///     Returns the next hit object in the hit objects list, if any,
        ///     otherwise null, O(1). Optionally skips concurrent objects.
        /// </summary>
        public HitObject Next(bool skipConcurrent = false)
        {
            HitObject next = null;

            for (var i = hitObjectIndex + 1; i < beatmap.HitObjects.Count; ++i)
            {
                next = beatmap.HitObjects[i];

                if (!skipConcurrent || !next.time.AlmostEqual(time))
                    break;
            }

            return next;
        }

        /// <summary>
        ///     Returns the previous hit object in the hit objects list, if any,
        ///     otherwise null, O(1). Optionally skips concurrent objects.
        /// </summary>
        public HitObject Prev(bool skipConcurrent = false)
        {
            HitObject prev = null;

            for (var i = hitObjectIndex - 1; i >= 0; --i)
            {
                prev = beatmap.HitObjects[i];

                if (!skipConcurrent || !prev.time.AlmostEqual(time))
                    break;
            }

            return prev;
        }

        /// <summary>
        ///     Returns the previous hit object in the hit objects list, if any,
        ///     otherwise the first, O(1). Optionally skips concurrent objects.
        /// </summary>
        public HitObject PrevOrFirst(bool skipConcurrent = false) => Prev(skipConcurrent) ?? beatmap.HitObjects.FirstOrDefault();

        /*
         *  Star Rating
         */

        /// <summary>
        ///     <para>Returns the difference in time between the start of this object and the start of the previous object.</para>
        ///     Note: This always returns at least 50 ms, to mimic the star rating algorithm.
        /// </summary>
        public double GetPrevDeltaStartTime() =>
            // Smallest value is 50 ms for pp calc as a safety measure apparently,
            // it's equivalent to 375 BPM streaming speed.
            Math.Max(50, time - PrevOrFirst().time);

        /// <summary>
        ///     <para>
        ///         Returns the distance between the edges of the hit circles for the start of this object and the start of the
        ///         previous object.
        ///     </para>
        ///     Note: This adds a bonus scaling factor for small circle sizes, to mimic the star rating algorithm.
        /// </summary>
        public double GetPrevStartDistance()
        {
            double radius = beatmap.DifficultySettings.GetCircleRadius();

            // We will scale distances by this factor, so we can assume a uniform CircleSize among beatmaps.
            var scalingFactor = 52 / radius;

            // small circle bonus
            if (radius < 30)
                scalingFactor *= 1 + Math.Min(30 - radius, 5) / 50;

            var prevPosition = PrevOrFirst().Position;
            double prevDistance = (Position - prevPosition).Length();

            return prevDistance * scalingFactor;
        }

        /*
         *  Utility
         */

        /// <summary> Returns whether a hit object code has the given type. </summary>
        public static bool HasType(string[] args, Types type) => ((Types)int.Parse(args[3]) & type) != 0;

        public bool HasType(Types type) => (this.type & type) != 0;

        /// <summary> Returns whether the hit object has a hit sound, or optionally a certain type of hit sound. </summary>
        public bool HasHitSound(HitSounds? hitSound = null) => hitSound == null ? this.hitSound > 0 : (this.hitSound & hitSound) != 0;

        /// <summary> Returns the difference in time between the start of this object and the end of the previous object. </summary>
        public double GetPrevDeltaTime() => time - Prev().GetEndTime();

        /// <summary> Returns the difference in distance between the start of this object and the end of the previous object. </summary>
        public double GetPrevDistance()
        {
            var prevObject = Prev();

            var prevPosition = prevObject.Position;

            if (prevObject is Slider slider)
                prevPosition = slider.EndPosition;

            return (Position - prevPosition).Length();
        }

        /// <summary>
        ///     Returns the points in time where heads, tails or reverses exist (i.e. the start, end or reverses of any
        ///     object).
        /// </summary>
        public IEnumerable<double> GetEdgeTimes()
        {
            // Head counts as an edge.
            yield return time;

            if (this is Slider slider)
                for (var i = 0; i < slider.EdgeAmount; ++i)
                    yield return time + slider.GetCurveDuration() * (i + 1);

            if (this is Spinner spinner)
                yield return spinner.endTime;

            if (this is HoldNote holdNote)
                yield return holdNote.endTime;
        }

        /// <summary> Returns the custom index for the object, if any, otherwise for the line, if any, otherwise 1. </summary>
        public int GetCustomIndex(TimingLine line = null)
        {
            if (line == null)
                line = beatmap.GetTimingLine(time);

            return customIndex ?? line?.CustomIndex ?? 1;
        }

        /// <summary> Returns the effective sampleset of the hit object (body for sliders), optionally prioritizing the addition. </summary>
        public HitSample.SamplesetType GetSampleset(bool additionOverrides = false, double? specificTime = null, bool isSliderTick = false)
        {
            if (additionOverrides && addition != HitSample.SamplesetType.Auto)
                return addition;

            // Inherits from timing line if auto.
            return sampleset == HitSample.SamplesetType.Auto ? beatmap.GetTimingLine(specificTime ?? time, true, isSliderTick).Sampleset : sampleset;
        }

        /// <summary>
        ///     Returns the effective sampleset of the head of the object, if applicable, otherwise null, optionally prioritizing
        ///     the addition.
        ///     Spinners have no start sample.
        /// </summary>
        public HitSample.SamplesetType? GetStartSampleset(bool additionOverrides = false) =>
            (this as Slider)?.GetStartSampleset(additionOverrides) ?? (this is Spinner ? null : (HitSample.SamplesetType?)GetSampleset(additionOverrides));

        /// <summary>
        ///     Returns the effective sampleset of the tail of the object, if applicable, otherwise null, optionally prioritizing
        ///     the addition.
        ///     Spinners have no start sample.
        /// </summary>
        public virtual HitSample.SamplesetType? GetEndSampleset(bool additionOverrides = false) =>
            (this as Slider)?.GetEndSampleset(additionOverrides) ?? (this is Spinner ? (HitSample.SamplesetType?)GetSampleset(additionOverrides) : null);

        /// <summary>
        ///     Returns the hit sound(s) of the head of the object, if applicable, otherwise null.
        ///     Spinners have no start sample.
        /// </summary>
        public HitSounds? GetStartHitSound() =>
            (this as Slider)?.StartHitSound ?? (this is Spinner ? null : (HitSounds?)hitSound);

        /// <summary>
        ///     Returns the hit sound(s) of the tail of the object, if it applicable, otherwise null.
        ///     Circles and hold notes have no end sample.
        /// </summary>
        public HitSounds? GetEndHitSound() => (this as Slider)?.EndHitSound ?? (this as Spinner)?.hitSound;

        /// <summary>
        ///     Returns the hit sound(s) of the slide of the object, if applicable, otherwise null.
        ///     Circles, hold notes and spinners have no sliderslide.
        /// </summary>
        public HitSounds? GetSliderSlide() => (this as Slider)?.hitSound;

        /// <summary>
        ///     Returns all individual hit sounds used by a specific hit sound instnace,
        ///     excluding <see cref="HitSounds.None" />.
        /// </summary>
        private IEnumerable<HitSounds> SplitHitSound(HitSounds hitSound)
        {
            foreach (HitSounds individualHitSound in Enum.GetValues(typeof(HitSounds)))
                if ((hitSound & individualHitSound) != 0 && individualHitSound != HitSounds.None)
                    yield return individualHitSound;
        }

        private HitSample GetEdgeSample(double time, HitSample.SamplesetType? sampleset, HitSounds? hitSound)
        {
            var line = beatmap.GetTimingLine(time, true);

            return new HitSample(line.CustomIndex, sampleset ?? line.Sampleset, hitSound, HitSample.HitSourceType.Edge, time);
        }

        /// <summary> Returns all used combinations of customs, samplesets and hit sounds for this object. </summary>
        protected IEnumerable<HitSample> GetUsedHitSamples()
        {
            if (beatmap == null)
                // Without a beatmap, we don't know which samples are going to be used, so leave this empty.
                yield break;

            var mode = beatmap.GeneralSettings.mode;

            // Standard can be converted into taiko, so taiko samples could be used there too.
            if (mode == Beatmap.Mode.Taiko || mode == Beatmap.Mode.Standard)
                foreach (var sample in GetUsedHitSamplesTaiko())
                    yield return sample;

            if (mode != Beatmap.Mode.Taiko)
                foreach (var sample in GetUsedHitSamplesNonTaiko())
                    yield return sample;
        }

        /// <summary>
        ///     Returns all used combinations of customs, samplesets and hit sounds for this object.
        ///     This assumes the game mode is not taiko (special rules apply to taiko only).
        /// </summary>
        private IEnumerable<HitSample> GetUsedHitSamplesNonTaiko()
        {
            // Spinners have no impact sound.
            if (!(this is Spinner))
            {
                // Head
                foreach (var splitStartHitSound in SplitHitSound(GetStartHitSound().GetValueOrDefault()))
                    yield return GetEdgeSample(time, GetStartSampleset(true), splitStartHitSound);

                yield return GetEdgeSample(time, GetStartSampleset(), HitSounds.Normal);
            }

            // Hold notes can not have a hit sounds on their tails.
            if (!(this is HoldNote))
            {
                // Tail
                foreach (var splitEndHitSound in SplitHitSound(GetEndHitSound().GetValueOrDefault()))
                    yield return GetEdgeSample(GetEndTime(), GetEndSampleset(true), splitEndHitSound);

                yield return GetEdgeSample(GetEndTime(), GetEndSampleset(), HitSounds.Normal);
            }

            if (this is Slider slider)
            {
                // Reverse
                for (var i = 0; i < slider.ReverseHitSounds.Count; ++i)
                {
                    HitSounds? reverseHitSound = slider.ReverseHitSounds.ElementAt(i);

                    var theoreticalStart = time - beatmap.GetTheoreticalUnsnap(time);
                    double reverseTime = Timestamp.Round(theoreticalStart + slider.GetCurveDuration() * (i + 1));

                    foreach (var splitReverseHitSound in SplitHitSound(reverseHitSound.GetValueOrDefault()))
                        yield return GetEdgeSample(reverseTime, slider.GetReverseSampleset(i, true), splitReverseHitSound);

                    yield return GetEdgeSample(reverseTime, slider.GetReverseSampleset(i), HitSounds.Normal);
                }

                var lines = beatmap.TimingLines.Where(line => line.Offset > slider.time && line.Offset <= slider.EndTime).ToList();

                lines.Add(beatmap.GetTimingLine(slider.time, true));

                // Body, only applies to standard. Catch has droplets instead of body. Taiko and mania have a body but play no background sound.
                if (beatmap.GeneralSettings.mode == Beatmap.Mode.Standard)
                    foreach (var line in lines)
                    {
                        // Priority: object sampleset > line sampleset
                        // The addition is ignored for sliderslides, it seems.
                        var effectiveSampleset = sampleset != HitSample.SamplesetType.Auto ? sampleset : line.Sampleset;

                        // The regular sliderslide will always play regardless of using sliderwhistle.
                        yield return new HitSample(line.CustomIndex, effectiveSampleset, HitSounds.None, HitSample.HitSourceType.Body, line.Offset);

                        // Additions are not ignored for sliderwhistles, however.
                        if (slider.hitSound == HitSounds.Whistle)
                            effectiveSampleset = addition != HitSample.SamplesetType.Auto ? addition : effectiveSampleset;

                        if (hitSound != HitSounds.None)
                            yield return new HitSample(line.CustomIndex, effectiveSampleset, hitSound, HitSample.HitSourceType.Body, line.Offset);
                    }

                // Tick, only applies to standard and catch. Mania has no ticks, taiko sliders play regular impacts.
                if (beatmap.GeneralSettings.mode == Beatmap.Mode.Standard || beatmap.GeneralSettings.mode == Beatmap.Mode.Catch)
                    foreach (var tickTime in slider.SliderTickTimes)
                    {
                        // Our `sliderTickTimes` are approximate values, the game chooses sampleset based on precise tick times, so we should too.
                        // Also, slider ticks have 2 ms of hit sound leniency, unlike the 5 ms for circles and other objects.
                        var preciseTickTime = tickTime + beatmap.GetTheoreticalUnsnap(tickTime);
                        var line = beatmap.GetTimingLine(preciseTickTime, true, true);

                        // If no line exists, we use the default settings.
                        var customIndex = line?.CustomIndex ?? 1;

                        // Unlike the slider body (for sliderwhistles) and edges, slider ticks are unaffected by additions.
                        var sampleset = GetSampleset(false, preciseTickTime, true);

                        // Defaults to normal if none is set (before any timing line).
                        if (sampleset == HitSample.SamplesetType.Auto)
                            sampleset = HitSample.SamplesetType.Normal;

                        yield return new HitSample(customIndex, sampleset, null, HitSample.HitSourceType.Tick, tickTime);
                    }
            }
        }

        /// <summary>
        ///     Returns all used combinations of customs, samplesets and hit sounds for this object.
        ///     Assumes the game mode is taiko (special rules apply).
        ///     <br></br><br></br>
        ///     Special Rules:<br></br>
        ///     - taiko-hitwhistle plays on big kat <br></br>
        ///     - taiko-hitfinish plays on big don <br></br>
        ///     - taiko-hitclap and taiko-hitnormal are always used as they play whenever the user presses keys
        /// </summary>
        public IEnumerable<HitSample> GetUsedHitSamplesTaiko()
        {
            var line = beatmap.GetTimingLine(time, true);
            if (line == null)
                throw new Exception("Beatmap has no timing line.");

            yield return new HitSample(line.CustomIndex, line.Sampleset, HitSounds.Clap, HitSample.HitSourceType.Edge, line.Offset, true);
            yield return new HitSample(line.CustomIndex, line.Sampleset, HitSounds.Normal, HitSample.HitSourceType.Edge, line.Offset, true);

            var isKat = HasHitSound(HitSounds.Clap) || HasHitSound(HitSounds.Whistle);
            var isBig = HasHitSound(HitSounds.Finish);

            HitSounds hitSound;

            if (isBig)
                if (isKat) hitSound = HitSounds.Whistle;
                else hitSound = HitSounds.Finish;
            else if (isKat) hitSound = HitSounds.Clap;
            else hitSound = HitSounds.Normal;

            // In case the hit object's custom index/sampleset/additions are different from the timing line's.
            yield return new HitSample(GetCustomIndex(line), GetSampleset(true), hitSound, HitSample.HitSourceType.Edge, time, true);
        }

        /// <summary>
        ///     Returns all potentially used hit sound file names (should they be
        ///     in the song folder) for this object without extension.
        /// </summary>
        public IEnumerable<string> GetUsedHitSoundFileNames()
        {
            // If you supply a specific hit sound file to the object, this file will replace all
            // other hit sounds, customs, etc, including the hit normal.
            string specificHsFileName = null;

            if (filename != null)
            {
                if (filename.Contains("."))
                    specificHsFileName = filename.Substring(0, filename.IndexOf(".", StringComparison.Ordinal));
                else
                    specificHsFileName = filename;
            }

            if (specificHsFileName != null)
                return new List<string> { specificHsFileName };

            var usedHitSoundFileNames = usedHitSamples.Select(sample => sample.GetFileName()).Where(name => name != null).Distinct();

            return usedHitSoundFileNames;
        }

        /// <summary> Returns the end time of the hit object, or the start time if no end time exists. </summary>
        public double GetEndTime() =>
            // regardless of circle/slider/spinner/hold note, finds the end of the object
            (this as Slider)?.EndTime ?? (this as Spinner)?.endTime ?? (this as HoldNote)?.endTime ?? time;

        /// <summary> Returns the length of the hit object, if it has one, otherwise 0. </summary>
        public double GetLength() => GetEndTime() - time;

        /// <summary>
        ///     Returns the name of the object part at the given time, for example "Slider head", "Slider reverse", "Circle"
        ///     or "Spinner tail".
        /// </summary>
        public string GetPartName(double time)
        {
            // Checks within 2 ms leniency in case of decimals or unsnaps.
            bool isClose(double edgeTime, double otherTime) =>
                edgeTime <= otherTime + 2 && edgeTime >= otherTime - 2;

            var edgeType = isClose(this.time, time) ? "head" :
                isClose(GetEndTime(), time) || isClose(GetEdgeTimes().Last(), time) ? "tail" :
                GetEdgeTimes().Any(edgeTime => isClose(edgeTime, time)) ? "reverse" : "body";

            return GetObjectType() + (!(this is Circle) ? " " + edgeType : "");
        }

        /// <summary> Returns the name of the object in general, for example "Slider", "Circle", "Hold note", etc. </summary>
        public string GetObjectType() =>
            // Creating a hit object instance rather than circle, slider, etc will prevent polymorphism, so we check the type as well.
            this is Slider || type.HasFlag(Types.Slider) ? "Slider" :
            this is Circle || type.HasFlag(Types.Circle) ? "Circle" :
            this is Spinner || type.HasFlag(Types.Spinner) ? "Spinner" :
            this is HoldNote || type.HasFlag(Types.ManiaHoldNote) ? "Hold note" : "Unknown object";

        public override string ToString() => time + " ms: " + GetObjectType() + " at (" + Position.X + "; " + Position.Y + ")";
    }
}