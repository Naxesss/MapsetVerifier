using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using MapsetVerifier.Parser.Objects.Events;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Settings;
using MapsetVerifier.Parser.StarRating;
using MapsetVerifier.Parser.StarRating.Osu;
using MapsetVerifier.Parser.StarRating.Taiko;
using MapsetVerifier.Parser.Statics;
using MathNet.Numerics;

namespace MapsetVerifier.Parser.Objects
{
    public class Beatmap
    {
        /// <summary> Which type of difficulty level the beatmap is considered. </summary>
        public enum Difficulty
        {
            Easy,
            Normal,
            Hard,
            Insane,
            Expert,
            Ultra
        }

        /// <summary> Which type of game mode the beatmap is for. </summary>
        public enum Mode
        {
            Standard,
            Taiko,
            Catch,
            Mania
        }

        private static readonly int[] divisors = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 16 };

        /// <summary>
        ///     A list of aliases for difficulty levels. Can't be ambigious with named top diffs, so something
        ///     like "Lunatic", "Another", or "Special" which could be either Insane or top diff is no good.
        ///     See https://osu.ppy.sh/help/wiki/Ranking_Criteria/Difficulty_Naming for reference.
        /// </summary>
        private static readonly Dictionary<Mode, Dictionary<Difficulty, IEnumerable<string>>> nameDiffPairs = new()
        {
            {
                Mode.Standard,
                new Dictionary<Difficulty, IEnumerable<string>>
                {
                    //                                       osu!                         Common Variations
                    { Difficulty.Easy, new List<string> { "Beginner", "Easy", "Novice" } },
                    { Difficulty.Normal, new List<string> { "Basic", "Normal", "Medium", "Intermediate" } },
                    { Difficulty.Hard, new List<string> { "Advanced", "Hard" } },
                    { Difficulty.Insane, new List<string> { "Hyper", "Insane" } },
                    { Difficulty.Expert, new List<string> { "Expert", "Extra", "Extreme" } }
                }
            },
            {
                Mode.Taiko,
                new Dictionary<Difficulty, IEnumerable<string>>
                {
                    //                                       osu!taiko/Taiko no Tatsujin
                    { Difficulty.Easy, new List<string> { "Kantan" } },
                    { Difficulty.Normal, new List<string> { "Futsuu" } },
                    { Difficulty.Hard, new List<string> { "Muzukashii" } },
                    { Difficulty.Insane, new List<string> { "Oni" } },
                    { Difficulty.Expert, new List<string> { "Inner Oni", "Ura Oni" } },
                    { Difficulty.Ultra, new List<string> { "Hell Oni" } }
                }
            },
            {
                Mode.Catch,
                new Dictionary<Difficulty, IEnumerable<string>>
                {
                    //                                       osu!catch
                    { Difficulty.Easy, new List<string> { "Cup" } },
                    { Difficulty.Normal, new List<string> { "Salad" } },
                    { Difficulty.Hard, new List<string> { "Platter" } },
                    { Difficulty.Insane, new List<string> { "Rain" } },
                    { Difficulty.Expert, new List<string> { "Overdose", "Deluge" } }
                }
            },
            {
                Mode.Mania,
                new Dictionary<Difficulty, IEnumerable<string>>
                {
                    //                                       osu!mania/DJMAX (+EZ2DJ/AC)  Beatmania IIDX    SVDX
                    { Difficulty.Easy, new List<string> { "EZ", "Beginner", "Basic" } },
                    { Difficulty.Normal, new List<string> { "NM", "Normal", "Novice" } },
                    { Difficulty.Hard, new List<string> { "HD", "Hyper", "Advanced" } },
                    { Difficulty.Insane, new List<string> { "MX", "SHD", "Another", "Exhaust" } },
                    {
                        Difficulty.Expert,
                        new List<string> { "SC", "EX", "Black Another", "Infinite", "Gravity", "Heavenly" }
                    }
                }
            }
        };

        public string Code { get; }
        public string SongPath { get; }
        public string MapPath { get; }

        // Star Rating
        public double StarRating { get; }
        public DifficultyAttributes DifficultyAttributes { get; }

        // Settings
        public GeneralSettings GeneralSettings { get; }
        public MetadataSettings MetadataSettings { get; }
        public DifficultySettings DifficultySettings { get; }
        public ColourSettings ColourSettings { get; }
        
        // Events
        public List<Background> Backgrounds { get; }
        public List<Video> Videos { get; }
        public List<Break> Breaks { get; }
        public List<Sprite> Sprites { get; }
        public List<Sample> Samples { get; }
        public List<Animation> Animations { get; }

        // Objects
        public List<HitObject> HitObjects { get; }
        public List<TimingLine> TimingLines { get; }

        public Beatmap(string code, double? starRating = null, string songPath = null, string mapPath = null)
        {
            Code = code;
            SongPath = songPath;
            MapPath = mapPath;

            var lines = code.Split(new[] { "\n" }, StringSplitOptions.None);

            GeneralSettings = ParserStatic.GetSettings(lines, "General", sectionLines => new GeneralSettings(sectionLines));
            MetadataSettings = ParserStatic.GetSettings(lines, "Metadata", sectionLines => new MetadataSettings(sectionLines));
            DifficultySettings = ParserStatic.GetSettings(lines, "Difficulty", sectionLines => new DifficultySettings(sectionLines));
            ColourSettings = ParserStatic.GetSettings(lines, "Colours", sectionLines => new ColourSettings(sectionLines));

            // event type 3 seems to be "background colour transformation" https://i.imgur.com/Tqlz3s5.png

            Backgrounds = GetEvents(lines, new List<string> { "Background", "0" }, args => new Background(args));
            Videos = GetEvents(lines, new List<string> { "Video", "1" }, args => new Video(args));
            Breaks = GetEvents(lines, new List<string> { "Break", "2" }, args => new Break(args));
            Sprites = GetEvents(lines, new List<string> { "Sprite", "4" }, args => new Sprite(args));
            Samples = GetEvents(lines, new List<string> { "Sample", "5" }, args => new Sample(args));
            Animations = GetEvents(lines, new List<string> { "Animation", "6" }, args => new Animation(args));

            TimingLines = GetTimingLines(lines);
            HitObjects = GetHitobjects(lines);

            if (GeneralSettings.mode == Mode.Standard)
                // Stacking is standard-only.
                ApplyStacking();

            if (starRating != null)
            {
                StarRating = starRating.Value;

                return;
            }

            var attributes = GeneralSettings.mode switch
            {
                Mode.Standard => new OsuDifficultyCalculator(this).Calculate(),
                Mode.Taiko => new TaikoDifficultyCalculator(this).Calculate(),
                _ => null
            };

            if (attributes == null)
                return;

            DifficultyAttributes = attributes;
            StarRating = attributes.StarRating;
        }

        public static void ClearCache()
        {
            // Includes all types that can be given to `GetTimingLine` methods.
            ThreadSafeCacheHelper<TimingLine>.cache.Clear();
            ThreadSafeCacheHelper<InheritedLine>.cache.Clear();
            ThreadSafeCacheHelper<UninheritedLine>.cache.Clear();

            // Includes all types that can be given to `GetHitObject` methods.
            ThreadSafeCacheHelper<HitObject>.cache.Clear();
            ThreadSafeCacheHelper<Circle>.cache.Clear();
            ThreadSafeCacheHelper<Slider>.cache.Clear();
            ThreadSafeCacheHelper<Spinner>.cache.Clear();
            ThreadSafeCacheHelper<HoldNote>.cache.Clear();
        }

        private List<T> GetOrAdd<T>(Type t, Func<List<T>> func)
        {
            (string, Type) key = (MapPath, t);

            if (!ThreadSafeCacheHelper<T>.cache.ContainsKey(key))
                ThreadSafeCacheHelper<T>.cache[key] = func();

            return ThreadSafeCacheHelper<T>.cache[key];
        }

        /*
         *  Stacking Methods
         */

        /// <summary> Applies stacking for objects in the beatmap, updating the stack index and position values. </summary>
        private void ApplyStacking()
        {
            bool wasChanged;

            do
            {
                wasChanged = false;

                // Only hit objects that can be stacked can cause other objects to be stacked.
                var stackableHitObjects = HitObjects.OfType<Stackable>().ToList();

                for (var i = 0; i < stackableHitObjects.Count - 1; ++i)
                    for (var j = i + 1; j < stackableHitObjects.Count; ++j)
                    {
                        var hitObject = stackableHitObjects[i];
                        var otherHitObject = stackableHitObjects[j];

                        if (!MeetsStackTime(hitObject, otherHitObject))
                            break;

                        if (hitObject is Circle || otherHitObject is Circle)
                        {
                            if (ShouldStack(hitObject, otherHitObject))
                            {
                                if (otherHitObject is Slider || otherHitObject.isOnSlider)
                                    hitObject.isOnSlider = true;

                                // Sliders are never less than 0 stack index.
                                // Circles go below 0 when stacked under slider tails.
                                if (hitObject.stackIndex < 0 && !hitObject.isOnSlider)
                                {
                                    // Objects stacked under slider tails will continue to stack downwards.
                                    otherHitObject.stackIndex = hitObject.stackIndex - 1;
                                    wasChanged = true;

                                    break;
                                }

                                hitObject.stackIndex = otherHitObject.stackIndex + 1;
                                wasChanged = true;

                                break;
                            }

                            if (IsStacked(hitObject, otherHitObject)) break;
                        }

                        if (!(hitObject is Slider slider))
                            continue;

                        if (ShouldStackTail(slider, otherHitObject))
                        {
                            // Slider tail on circle means the circle moves down,
                            // whereas slider tail on slider head means the first slider moves up.
                            // Only sliders later in time can move sliders earlier in time.
                            if (otherHitObject is Slider || otherHitObject.isOnSlider)
                            {
                                slider.isOnSlider = true;
                                slider.stackIndex = otherHitObject.stackIndex + 1;
                            }
                            else
                            {
                                otherHitObject.stackIndex = slider.stackIndex - 1;
                            }

                            wasChanged = true;

                            break;
                        }

                        if (IsStackedTail(slider, otherHitObject)) break;
                    }
            } while (wasChanged);
        }

        /// <summary> Returns whether two stackable objects could be stacked. </summary>
        private bool CanStack(Stackable stackable, Stackable otherStackable)
        {
            var isNearInTime = MeetsStackTime(stackable, otherStackable);
            var isNearInSpace = MeetsStackDistance(stackable, otherStackable);

            return isNearInTime && isNearInSpace;
        }

        /// <summary> Returns whether two stackable objects are currently stacked. </summary>
        private bool IsStacked(Stackable stackable, Stackable otherStackable)
        {
            var isAlreadyStacked = stackable.stackIndex == otherStackable.stackIndex + 1;

            return CanStack(stackable, otherStackable) && isAlreadyStacked;
        }

        /// <summary> Returns whether two stackable objects should be stacked, but currently are not. </summary>
        private bool ShouldStack(Stackable stackable, Stackable otherStackable) => CanStack(stackable, otherStackable) && !IsStacked(stackable, otherStackable);

        /// <summary>
        ///     Returns whether a stackable following a slider could be stacked under the tail
        ///     (or over in case of slider and slider).
        /// </summary>
        private bool CanStackTail(Slider slider, Stackable stackable)
        {
            double distanceSq = Vector2.DistanceSquared(stackable.UnstackedPosition, slider.EdgeAmount % 2 == 0 ? slider.UnstackedPosition : slider.UnstackedEndPosition);

            var isNearInTime = MeetsStackTime(slider, stackable);
            var isNearInSpace = distanceSq < 3 * 3;

            return isNearInTime && isNearInSpace && slider.time < stackable.time;
        }

        /// <summary>
        ///     Returns whether a stackable following a slider is stacked under the tail
        ///     (or over in case of slider and slider).
        /// </summary>
        private bool IsStackedTail(Slider slider, Stackable stackable)
        {
            var isAlreadyStacked = slider.stackIndex == stackable.stackIndex + 1;

            return CanStackTail(slider, stackable) && isAlreadyStacked;
        }

        /// <summary>
        ///     Returns whether a stackable following a slider should be stacked under the slider tail
        ///     (or slider over the head in case of slider and slider), but currently is not.
        /// </summary>
        private bool ShouldStackTail(Slider slider, Stackable stackable) => CanStackTail(slider, stackable) && !IsStackedTail(slider, stackable);

        /// <summary>
        ///     Returns whether two stackable objects are close enough in time to be stacked. Measures from start to start
        ///     time.
        /// </summary>
        private bool MeetsStackTime(Stackable stackable, Stackable otherStackable) => otherStackable.time - stackable.time <= StackTimeThreshold();

        /// <summary> Returns whether two stackable objects are close enough in space to be stacked. Measures from head to head. </summary>
        private static bool MeetsStackDistance(Stackable stackable, Stackable otherStackable) => Vector2.DistanceSquared(stackable.UnstackedPosition, otherStackable.UnstackedPosition) < 3 * 3;

        /// <summary> Returns how far apart in time two objects can be and still be able to stack. </summary>
        private double StackTimeThreshold() => DifficultySettings.GetFadeInTime() * GeneralSettings.stackLeniency * 0.1;

        /*
         *  Helper Methods
         */

        /// <summary>
        ///     Returns the element in the sorted list where the given time is greater
        ///     than the element time, but less than the next element time (e.g. the line in effect
        ///     at some point in time, if we give a list of timing lines).
        ///     <br></br><br></br>
        ///     Since the list is sorted, we can use the Binary Search algorithm here to get
        ///     O(logn) time complexity, instead of O(n), which we would get from linear searching.
        /// </summary>
        private static int BinaryTimeSearch<T>(IReadOnlyList<T> sortedList, Func<T, double> Time, double time, int start = 0, int end = int.MaxValue)
        {
            while (true)
            {
                if (start < 0)
                    // Given time is before the list starts, so there is no current element.
                    return -1;

                if (end > sortedList.Count - 1)
                    end = sortedList.Count - 1;

                if (end < 0)
                    end = 0;

                if (start == end)
                    // Given time is after the list ends, so the last element in the list must be the current.
                    return end;

                var i = start + (end - start) / 2;

                var cur = sortedList[i];
                var next = sortedList[i + 1];

                if (time >= Time(cur) && time < Time(next))
                    return i;

                if (time >= Time(next))
                    // Element is too far back, move forward.
                    start = i + 1;

                else
                    // Element is too far forward, move back.
                    end = i - 1;
            }
        }

        /// <summary>
        ///     Returns the timing line currently in effect at the given time, if any, otherwise the first, O(logn).
        ///     Optionally with a 5 ms backward leniency for hit sounding, or 2 ms for slider ticks.
        /// </summary>
        public TimingLine GetTimingLine(double time, bool hitSoundLeniency = false, bool isLeniencyForSliderTick = false) =>
            GetTimingLine<TimingLine>(time, hitSoundLeniency, isLeniencyForSliderTick);

        /// <summary> Same as <see cref="GetTimingLine" /> except only considers objects of a given type. </summary>
        public T GetTimingLine<T>(double time, bool hitSoundLeniency = false, bool isLeniencyForSliderTick = false) where T : TimingLine
        {
            // Cache the results per generic type; timing line and hit object lists are immutable,
            // meaning we always expect the same result from the same input.
            var list = GetOrAdd(typeof(T), () => TimingLines.OfType<T>().ToList());

            if (list.Count == 0)
                return null;

            var index = BinaryTimeSearch(list, line => line.Offset - (hitSoundLeniency ? isLeniencyForSliderTick ? 2 : 5 : 0), time);

            if (index < 0)
                // Before any timing line starts, so return first line.
                return list[0];

            return list[index];
        }

        /// <summary> Returns the next timing line, if any, otherwise null, O(logn). </summary>
        public TimingLine GetNextTimingLine(double time) => GetNextTimingLine<TimingLine>(time);

        /// <summary> Same as <see cref="GetNextTimingLine" /> except only considers objects of a given type. </summary>
        public T GetNextTimingLine<T>(double time) where T : TimingLine
        {
            var list = GetOrAdd(typeof(T), () => TimingLines.OfType<T>().ToList());

            if (list.Count == 0)
                return null;

            var index = BinaryTimeSearch(list, line => line.Offset, time);

            if (index < 0)
                // Before any timing line starts, so return first line.
                return list[0];

            if (index + 1 >= list.Count)
                // After last timing line, so there's no next.
                return null;

            return list[index + 1];
        }

        /// <summary> Returns the current or previous hit object if any, otherwise the first, O(logn). </summary>
        public HitObject GetHitObject(double time) => GetHitObject<HitObject>(time);

        /// <summary> Same as <see cref="GetHitObject" /> except only considers objects of a given type. </summary>
        public T GetHitObject<T>(double time) where T : HitObject
        {
            var list = GetOrAdd(typeof(T), () => HitObjects.OfType<T>().ToList());

            if (list.Count == 0)
                return null;

            var index = BinaryTimeSearch(list, obj => obj.time, time);

            if (index < 0)
                // Before first hit object, so return first one.
                return list[0];

            return list[index];
        }

        /// <summary> Returns the previous hit object if any, otherwise the first, O(logn). </summary>
        public HitObject GetPrevHitObject(double time) => GetPrevHitObject<HitObject>(time);

        /// <summary> Same as <see cref="GetPrevHitObject" /> except only considers objects of a given type. </summary>
        public T GetPrevHitObject<T>(double time) where T : HitObject
        {
            var list = GetOrAdd(typeof(T), () => HitObjects.OfType<T>().ToList());

            if (list.Count == 0)
                return null;

            var index = BinaryTimeSearch(list, obj => obj.time, time);

            if (index - 1 < 0)
                // Before the first object, so return the first one.
                return list[0];

            if (list[index].GetEndTime() < time)
                // Directly in front of the previous object.
                return list[index];

            return list[index - 1];
        }

        /// <summary> Returns the next hit object after the current, if any, otherwise null, O(logn). </summary>
        public HitObject GetNextHitObject(double time) => GetNextHitObject<HitObject>(time);

        /// <summary> Same as <see cref="GetNextHitObject" /> except only considers objects of a given type. </summary>
        public T GetNextHitObject<T>(double time) where T : HitObject
        {
            var list = GetOrAdd(typeof(T), () => HitObjects.OfType<T>().ToList());

            if (list.Count == 0)
                return null;

            var index = BinaryTimeSearch(list, obj => obj.time, time);

            if (index < 0)
                // Before first hit object, so return first one.
                return list[0];

            if (index + 1 >= list.Count)
                // After last hit object, so there is no next.
                return null;

            return list[index + 1];
        }

        /// <summary> Returns the unsnap in ms of notes unsnapped by 2 ms or more, otherwise null. </summary>
        public double? GetUnsnapIssue(double time)
        {
            var thresholdUnrankable = 2;

            var unsnap = GetPracticalUnsnap(time);

            if (Math.Abs(unsnap) >= thresholdUnrankable)
                return unsnap;

            return null;
        }

        /// <summary> Returns the current combo colour number, starts at 0. </summary>
        public int GetComboColourIndex(double time)
        {
            var combo = 0;

            foreach (var hitObject in HitObjects)
            {
                if (hitObject.time > time)
                    break;

                // ignore spinners
                if (!hitObject.HasType(HitObject.Types.Spinner))
                {
                    var reverses = 0;

                    // has new combo
                    if (hitObject.HasType(HitObject.Types.NewCombo))
                        reverses += 1;

                    // accounts for the combo colour skips
                    for (var bit = 0x10; bit < 0x80; bit <<= 1)
                        if (((int)hitObject.type & bit) > 0)
                            reverses += (int)Math.Floor(bit / 16.0f);

                    // counts up and wraps around
                    for (var l = 0; l < reverses; l++)
                    {
                        combo += 1;

                        if (combo >= ColourSettings.combos.Count)
                            combo = 0;
                    }
                }
            }

            return combo;
        }

        /// <summary>
        ///     Same as <see cref="GetComboColourIndex" />, except accounts for a bug which makes the last registered colour in
        ///     the code the first number in the editor. Basically use for display purposes.
        /// </summary>
        public int GetDisplayedComboColourIndex(double time) => AsDisplayedComboColourIndex(GetComboColourIndex(time));

        /// <summary>
        ///     Accounts for a bug which makes the last registered colour in
        ///     the code the first number in the editor. Basically use for display purposes.
        /// </summary>
        public int AsDisplayedComboColourIndex(int zeroBasedIndex) => zeroBasedIndex == 0 ? ColourSettings.combos.Count : zeroBasedIndex;

        /// <summary> Returns whether a difficulty-specific storyboard is present, does not care about .osb files. </summary>
        public bool HasDifficultySpecificStoryboard()
        {
            if (Sprites.Count > 0 || Animations.Count > 0)
                return true;

            return false;
        }

        /// <summary>
        ///     Returns the interpreted difficulty level based on the star rating of the beatmap
        ///     (may be inaccurate since recent sr reworks were done), can optionally consider diff names.
        /// </summary>
        public Difficulty GetDifficulty(bool considerName = false)
        {
            Difficulty difficulty;

            if (StarRating < 2.0f) difficulty = Difficulty.Easy;
            else if (StarRating < 2.7f) difficulty = Difficulty.Normal;
            else if (StarRating < 4.0f) difficulty = Difficulty.Hard;
            else if (StarRating < 5.3f) difficulty = Difficulty.Insane;
            else if (StarRating < 6.5f) difficulty = Difficulty.Expert;
            else difficulty = Difficulty.Ultra;

            if (!considerName)
                return difficulty;

            return GetDifficultyFromName() ?? difficulty;
        }

        public Difficulty? GetDifficultyFromName()
        {
            var name = MetadataSettings.version;

            // Reverse order allows e.g. "Inner Oni"/"Black Another" to be looked for separately from just "Oni"/"Another".
            var pairs = nameDiffPairs[GeneralSettings.mode].Reverse();

            foreach (var pair in pairs)
                // Allows difficulty names such as "Normal...!??" and ">{(__HARD;)}" to be detected,
                // but still prevents "Normality" or similar inclusions.
                if (pair.Value.Any(value => new Regex(@$"(?i)(^| )[!-@\[-`{{-~]*{value}[!-@\[-`{{-~]*( |$)").IsMatch(name)))
                    return pair.Key;

            return null;
        }

        /// <summary>
        ///     Returns the name of the difficulty in a gramatically correct way, for example "an Easy" and "a Normal".
        ///     Mostly useful for adding in the middle of sentences.
        /// </summary>
        public string GetDifficultyName(Difficulty? difficulty = null)
        {
            switch (difficulty ?? GetDifficulty())
            {
                case Difficulty.Easy: return "an Easy";
                case Difficulty.Normal: return "a Normal";
                case Difficulty.Hard: return "a Hard";
                case Difficulty.Insane: return "an Insane";
                case Difficulty.Expert: return "an Expert";
                default: return "an Ultra";
            }
        }

        /// <summary> Returns the complete drain time of the beatmap, accounting for breaks. </summary>
        public double GetDrainTime()
        {
            if (HitObjects.Count > 0)
            {
                var startTime = HitObjects.First().time;
                var endTime = HitObjects.Last().GetEndTime();

                // remove breaks
                double breakReduction = 0;

                foreach (var @break in Breaks)
                    breakReduction += @break.GetDuration(this);

                return endTime - startTime - breakReduction;
            }

            return 0;
        }

        /// <summary>
        ///     Returns the play time of the beatmap, starting from the first object and ending at the end of the last
        ///     object.
        /// </summary>
        public double GetPlayTime()
        {
            if (HitObjects.Count > 0)
            {
                var startTime = HitObjects.First().time;
                var endTime = HitObjects.Last().GetEndTime();

                return endTime - startTime;
            }

            return 0;
        }

        /// <summary>
        ///     Returns the beat number from offset 0 at which the countdown would start, accounting for
        ///     countdown offset and speed. No countdown if less than 0.
        /// </summary>
        public double GetCountdownStartBeat()
        {
            // If there are no objects, this does not apply.
            if (GetHitObject(0) == null)
                return 0;

            // always 6 beats before the first, but the first beat can be cut by having the first beat 5 ms after 0.
            var line = GetTimingLine<UninheritedLine>(0);

            var firstBeatTime = line.Offset;

            while (firstBeatTime - line.msPerBeat > 0)
                firstBeatTime -= line.msPerBeat;

            var firstObjectTime = GetHitObject(0).time;
            var firstObjectBeat = Timestamp.Round((firstObjectTime - firstBeatTime) / line.msPerBeat);

            // Apparently double does not result in the countdown needing half as much time, but rather closer to 0.45 times as much.
            var countdownMultiplier = GeneralSettings.countdown == GeneralSettings.Countdown.None ? 1 :
                GeneralSettings.countdown == GeneralSettings.Countdown.Half ? 2 : 0.45;

            return firstObjectBeat - ((firstBeatTime > 5 ? 5 : 6) + GeneralSettings.countdownBeatOffset) * countdownMultiplier;
        }

        /// <summary> Returns how many ms into a beat the given time is. </summary>
        public double GetOffsetIntoBeat(double time)
        {
            var line = GetTimingLine<UninheritedLine>(time);

            // gets how many miliseconds into a beat we are
            var msOffset = time - line.Offset;
            var division = msOffset / line.msPerBeat;
            var fraction = division - (float)Math.Floor(division);
            var beatOffset = fraction * line.msPerBeat;

            return beatOffset;
        }

        /// <summary>
        ///     Returns the lowest possible beat snap divisor to get to the given time with less than 2 ms of unsnap, 0 if
        ///     unsnapped.
        /// </summary>
        public int GetLowestDivisor(double time)
        {
            var line = GetTimingLine<UninheritedLine>(time);

            double GetAbsoluteUnsnap(int divisor) => Math.Abs(GetPracticalUnsnap(time, divisor, line));

            var minUnsnap = divisors.Min(GetAbsoluteUnsnap);

            if (minUnsnap > 2)
                return 0;

            return divisors.First(divisor => GetAbsoluteUnsnap(divisor).AlmostEqual(minUnsnap));
        }

        /// <summary> Returns the unsnap ignoring all of the game's rounding and other approximations. Can be negative. </summary>
        public double GetTheoreticalUnsnap(double time)
        {
            var line = GetTimingLine<UninheritedLine>(time);

            double[] theoreticalUnsnaps =
            {
                GetTheoreticalUnsnap(time, 16, line),
                GetTheoreticalUnsnap(time, 12, line),
                GetTheoreticalUnsnap(time, 9, line),
                GetTheoreticalUnsnap(time, 7, line),
                GetTheoreticalUnsnap(time, 5, line)
            };

            // Assume the closest possible snapping & retain signed values.
            var minUnsnap = theoreticalUnsnaps.Min(Math.Abs);

            return theoreticalUnsnaps.First(unsnap => Math.Abs(unsnap).AlmostEqual(minUnsnap));
        }

        /// <summary>
        ///     Returns the unsnap, from the given snap divisor, ignoring all of the game's rounding and other approximations.
        ///     Optionally supply the uninherited line, instead of the method looking this up itself. The value returned is in
        ///     terms of
        ///     how much the object needs to be moved forwards in time to be snapped.
        /// </summary>
        public double GetTheoreticalUnsnap(double time, int divisor, UninheritedLine line = null)
        {
            line ??= GetTimingLine<UninheritedLine>(time);

            var beatOffset = GetOffsetIntoBeat(time);
            var currentFraction = beatOffset / line.msPerBeat;

            var desiredFraction = Math.Round(currentFraction * divisor) / divisor;
            var differenceFraction = currentFraction - desiredFraction;
            var theoreticalUnsnap = differenceFraction * line.msPerBeat;

            return theoreticalUnsnap;
        }

        /// <summary>
        ///     Returns the unsnap accounting for the way the game rounds (or more accurately doesn't round) snapping.
        ///     <para />
        ///     The value returned is in terms of how much the object needs to be moved forwards in time to be snapped.
        /// </summary>
        public double GetPracticalUnsnap(double time)
        {
            var line = GetTimingLine<UninheritedLine>(time);

            // We cannot simply round the closest theoretical snap, because while e.g.
            // abs(1.23) < abs(-1.70),
            // 35601 - (int)(35601 - 1.23) > 35601 - (int)(35601 - (-1.70)).
            // (Giving unsnaps of 2 ms and 1 ms respectively).
            double[] practicalUnsnaps =
            {
                GetPracticalUnsnap(time, 16, line),
                GetPracticalUnsnap(time, 12, line),
                GetPracticalUnsnap(time, 9, line),
                GetPracticalUnsnap(time, 7, line),
                GetPracticalUnsnap(time, 5, line)
            };

            // Assume the closest possible snapping & retain signed values.
            var minUnsnap = practicalUnsnaps.Min(Math.Abs);

            return practicalUnsnaps.First(unsnap => Math.Abs(unsnap).AlmostEqual(minUnsnap));
        }

        /// <summary>
        ///     Same as <see cref="GetTheoreticalUnsnap(double, int, UninheritedLine)" />, except accounts for the way
        ///     the game rounds ms times.
        /// </summary>
        public double GetPracticalUnsnap(double time, int divisor, UninheritedLine line = null) => time - Timestamp.Round(time - GetTheoreticalUnsnap(time, divisor, line));

        /// <summary> Returns the combo number (the number you see on the notes), of a given hit object. </summary>
        public int GetCombo(HitObject hitObject)
        {
            var combo = 1;

            // Adds a combo number for each object before this that isn't a new combo.
            var firstHitObject = HitObjects[0];

            while (hitObject != null)
            {
                var prevHitObject = hitObject.Prev();

                // The first object in the beatmap is always a new combo.
                // Spinners and their following objects are also always new comboed.
                if (hitObject.type.HasFlag(HitObject.Types.NewCombo) || hitObject is Spinner || prevHitObject is Spinner || hitObject == firstHitObject)
                    break;

                hitObject = prevHitObject;

                ++combo;
            }

            return combo;
        }

        /// <summary> Returns the hit object count divided by the drain time. </summary>
        public double GetObjectDensity() => HitObjects.Count / GetDrainTime();

        /// <summary> Returns the full audio file path the beatmap uses if any such file exists, otherwise null. </summary>
        public string GetAudioFilePath()
        {
            if (SongPath != null)
            {
                // read the mp3 file tags, if an audio file is specified
                var audioFileName = GeneralSettings.audioFileName;
                var mp3Path = SongPath + Path.DirectorySeparatorChar + audioFileName;

                if (audioFileName.Length > 0 && File.Exists(mp3Path))
                    return mp3Path;
            }

            // no audio file
            return null;
        }

        /// <summary> Returns the expected file name of the .osu based on the beatmap's metadata. </summary>
        public string GetOsuFileName()
        {
            var songArtist = MetadataSettings.GetFileNameFiltered(MetadataSettings.artist);
            var songTitle = MetadataSettings.GetFileNameFiltered(MetadataSettings.title);
            var songCreator = MetadataSettings.GetFileNameFiltered(MetadataSettings.creator);
            var version = MetadataSettings.GetFileNameFiltered(MetadataSettings.version);

            return $"{songArtist} - {songTitle} ({songCreator}) [{version}].osu";
        }

        /*
         *  Parser Methods
         */

        private List<T> GetEvents<T>(string[] lines, List<string> types, Func<string[], T> func)
        {
            // find all lines starting with any of types in the event section
            var foundTypes = new List<T>();

            ParserStatic.ApplySettings(lines, "Events", sectionLines =>
            {
                foreach (var line in sectionLines)
                    if (types.Any(type => line.StartsWith(type + ",")))
                        foundTypes.Add(func(line.Split(',')));
            });

            return foundTypes;
        }

        private List<TimingLine> GetTimingLines(string[] lines)
        {
            // Find the [TimingPoints] section and parse each timing line.
            var timingLines = ParserStatic.ParseSection(lines, "TimingPoints", line =>
            {
                var args = line.Split(',');

                return TimingLine.IsUninherited(args) ? new UninheritedLine(args, this) : (TimingLine)new InheritedLine(args, this);
            }).OrderBy(line => line.Offset).ThenBy(line => line is InheritedLine).ToList();

            // Initialize internal indicies for O(1) next/prev access.
            for (var i = 0; i < timingLines.Count; ++i)
                timingLines[i].SetTimingLineIndex(i);

            return timingLines;
        }

        private List<HitObject> GetHitobjects(string[] lines)
        {
            // find the [Hitobjects] section and parse each hitobject until empty line or end of file
            var hitObjects = ParserStatic.ParseSection(lines, "HitObjects", line =>
            {
                var args = line.Split(',');

                return HitObject.HasType(args, HitObject.Types.Circle) ? new Circle(args, this) :
                    HitObject.HasType(args, HitObject.Types.Slider) ? new Slider(args, this) :
                    HitObject.HasType(args, HitObject.Types.ManiaHoldNote) ? new HoldNote(args, this) : (HitObject)new Spinner(args, this);
            }).OrderBy(hitObject => hitObject.time).ToList();

            // Initialize internal indicies for O(1) next/prev access.
            for (var i = 0; i < hitObjects.Count; ++i)
                hitObjects[i].SetHitObjectIndex(i);

            return hitObjects;
        }

        /// <summary> Returns the beatmap as a string in the format "[Insane]", if the difficulty is called "Insane", for example. </summary>
        public override string ToString() => "[" + MetadataSettings.version + "]";

        /*
         *  Optimization Methods
         */

        private static class ThreadSafeCacheHelper<T>
        {
            // Works under the assumption that hit objects and timing lines are immutable per beatmap id, which is the case.
            internal static readonly ConcurrentDictionary<(string, Type), List<T>> cache = new();
        }
    }
}