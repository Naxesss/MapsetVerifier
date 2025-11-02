using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;

namespace MapsetVerifier.Parser.Statics
{
    internal static class SkinStatic
    {
        private static bool isInitialized;

        private static readonly string[] skinGeneral =
        [
            // cursor
            "cursor.png",
            "cursormiddle.png",
            "cursor-smoke.png",
            "cursortrail.png",
            // playfield
            "play-skip-{n}.png",
            "play-unranked.png",
            "multi-skipped.png",
            // pause screen
            "pause-overlay.png",
            "pause-overlay.jpg", // according to the wiki page these are the only two which have jpg alternatives
            "fail-background.png", "fail-background.jpg",
            "pause-back.png",
            "pause-continue.png",
            "pause-replay.png",
            "pause-retry.png",
            // scorebar
            "scorebar-bg.png",
            "scorebar-colour.png",
            "scorebar-colour-{n}.png",
            // score numbers
            "score-0.png",
            "score-1.png",
            "score-2.png",
            "score-3.png",
            "score-4.png",
            "score-5.png",
            "score-6.png",
            "score-7.png",
            "score-8.png",
            "score-9.png",
            "score-comma.png",
            "score-dot.png",
            "score-percent.png",
            "score-x.png",
            // ranking grades
            "ranking-XH-small.png",
            "ranking-X-small.png",
            "ranking-SH-small.png",
            "ranking-S-small.png",
            "ranking-A-small.png",
            "ranking-B-small.png",
            "ranking-C-small.png",
            "ranking-D-small.png",
            // score entry (used in the leaderboard while in gameplay)
            "scoreentry-0.png",
            "scoreentry-1.png",
            "scoreentry-2.png",
            "scoreentry-3.png",
            "scoreentry-4.png",
            "scoreentry-5.png",
            "scoreentry-6.png",
            "scoreentry-7.png",
            "scoreentry-8.png",
            "scoreentry-9.png",
            "scoreentry-comma.png",
            "scoreentry-dot.png",
            "scoreentry-percent.png",
            "scoreentry-x.png",
            // song selection (used in the in-game leaderboard, among other stuff like kiai)
            "menu-button-background.png",
            "selection-tab.png",
            "star2.png",
            // mod icons (appears in the top right of the screen in gameplay)
            "selection-mod-autoplay.png",
            "selection-mod-cinema.png",
            "selection-mod-doubletime.png",
            "selection-mod-easy.png",
            "selection-mod-flashlight.png",
            "selection-mod-halftime.png",
            "selection-mod-hardrock.png",
            "selection-mod-hidden.png",
            "selection-mod-nightcore.png",
            "selection-mod-nofail.png",
            "selection-mod-perfect.png",
            "selection-mod-suddendeath.png",
            "selection-mod-scorev2.png",
            // sounds in gameplay
            "applause.wav", "applause.mp3", "applause.ogg",
            "comboburst.wav", "comboburst.mp3", "comboburst.ogg",
            "combobreak.wav", "combobreak.mp3", "combobreak.ogg",
            "failsound.wav", "failsound.mp3", "failsound.ogg",
            // sounds in the pause screen
            "pause-loop.wav", "pause-loop.mp3", "pause-loop.ogg"
        ];

        private static readonly string[] skinStandard =
        [
            // hit bursts
            "hit0-{n}.png",
            "hit50-{n}.png",
            "hit100-{n}.png",
            "hit100k-{n}.png",
            "hit300-{n}.png",
            "hit300g-{n}.png",
            "hit300k-{n}.png",
            // mod icons exceptions
            "selection-mod-relax2.png",
            "selection-mod-spunout.png",
            "selection-mod-target.png", // currently only cutting edge
            // combo burst
            "comboburst.png",
            "comboburst-{n}.png",
            // default numbers, used for combos
            "default-0.png",
            "default-1.png",
            "default-2.png",
            "default-3.png",
            "default-4.png",
            "default-5.png",
            "default-6.png",
            "default-7.png",
            "default-8.png",
            "default-9.png",
            // hit circles
            "approachcircle.png",
            "hitcircle.png",
            "hitcircleoverlay.png",
            "hitcircleoverlay-{n}.png",
            "hitcircleselect.png",
            "followpoint.png",
            "followpoint-{n}.png",
            "lighting.png"
        ];

        private static readonly string[] skinMania =
        [
            // mod icons
            "selection-mod-fadein.png",
            "selection-mod-key1.png",
            "selection-mod-key2.png",
            "selection-mod-key3.png",
            "selection-mod-key4.png",
            "selection-mod-key5.png",
            "selection-mod-key6.png",
            "selection-mod-key7.png",
            "selection-mod-key8.png",
            "selection-mod-key9.png",
            "selection-mod-keycoop.png",
            "selection-mod-random.png",
            // hit bursts
            "mania-hit0.png",
            "mania-hit50.png",
            "mania-hit100.png",
            "mania-hit200.png",
            "mania-hit300.png",
            "mania-hit300g.png",
            "mania-hit0-{n}.png",
            "mania-hit50-{n}.png",
            "mania-hit100-{n}.png",
            "mania-hit200-{n}.png",
            "mania-hit300-{n}.png",
            "mania-hit300g-{n}.png",
            // combo bursts
            "comboburst-mania.png",
            "comboburst-mania-{n}.png",
            // stages
            "mania-stage-left.png",
            "mania-stage-right.png"
        ];

        private static readonly string[] skinTaiko =
        [
            // pippidon
            "pippidonclear.png",
            "pippidonfail.png",
            "pippidonidle.png",
            "pippidonkiai.png",
            "pippidonclear{n}.png",
            "pippidonfail{n}.png",
            "pippidonidle{n}.png",
            "pippidonkiai{n}.png",
            // hit bursts
            "taiko-hit0.png",
            "taiko-hit100.png",
            "taiko-hit100k.png",
            "taiko-hit300.png",
            "taiko-hit300k.png",
            "taiko-hit0-{n}.png",
            "taiko-hit100-{n}.png",
            "taiko-hit100k-{n}.png",
            "taiko-hit300-{n}.png",
            "taiko-hit300k-{n}.png",
            // notes
            "taikobigcircle.png",
            "taikobigcircleoverlay.png",
            "taikohitcircle.png",
            "taikohitcircleoverlay.png",
            "approachcircle.png",
            "lighting.png",
            "taikobigcircleoverlay-{n}.png",
            "taikohitcircleoverlay-{n}.png",
            // playfield (upper half)
            "taiko-slider.png", // beatmap skinnable, but "taiko-slider-fail.png" is not, so probably a bug
            "taiko-flower-group.png",
            "taiko-flower-group-{n}.png"
        ];

        private static readonly string[] skinTaikoSlider =
        [
            // drumrolls
            "taiko-roll-middle.png",
            "taiko-roll-end.png",
            "sliderscorepoint.png"
        ];

        private static readonly string[] skinTaikoSpinner =
        [
            // shaker
            "spinner-warning.png"
        ];

        private static readonly string[] skinCatch =
        [
            // hit burst exception, appears in both modes' result screens
            // it does but the beatmap-specific skins don't have an effect there
            // "hit0.png",
            // input overlay
            "inputoverlay-background.png",
            "inputoverlay-key.png",
            // catcher
            "fruit-catcher-idle.png",
            "fruit-catcher-fail.png",
            "fruit-catcher-kiai.png",
            "fruit-ryuuta.png",
            "fruit-catcher-idle-{n}.png",
            "fruit-catcher-fail-{n}.png",
            "fruit-catcher-kiai-{n}.png",
            "fruit-ryuuta-{n}.png",
            // comboburst
            "comboburst-fruits.png",
            "comboburst-fruits-{n}.png",
            // fruits
            "lighting.png",
            "fruit-pear.png",
            "fruit-pear-overlay.png",
            "fruit-grapes.png",
            "fruit-grapes-overlay.png",
            "fruit-apple.png",
            "fruit-apple-overlay.png",
            "fruit-orange.png",
            "fruit-orange-overlay.png",
            "fruit-orange-0.png", // can apparently be "animated", but only the first frame is actually used
            "fruit-orange-overlay-0.png"
        ];

        private static readonly string[] skinCatchSlider =
        [
            // according to my other note beatmap skins don't have an effect in the scoreboard
            // this doesn't really matter, though, since they're part of a set with the main catch files
            "fruit-drop.png",
            "fruit-drop-overlay.png"
        ];

        private static readonly string[] skinCatchSpinner =
        [
            // according to my other note beatmap skins don't have an effect in the scoreboard
            // this doesn't really matter, though, since they're part of a set with the main catch files
            "fruit-bananas.png",
            "fruit-bananas-overlay.png"
        ];

        private static readonly string[] skinNotMania =
        [
            // scorebar exception, bar is in a different position and excludes this element because of that
            // marker is currently unused (contradicting the wiki), but it's part of the scorebar skin set and may be used in the future
            "scorebar-marker.png",
            // these were meant to be overriden by the marker, but the marker currently does nothing and can even be excluded
            "scorebar-ki.png",
            "scorebar-kidanger.png",
            "scorebar-kidanger2.png",
            // mod icons exception, in mania there's no difference between something clicking for you and just using auto
            "selection-mod-relax.png"
        ];

        private static readonly string[] skinCountdown =
        [
            // playfield
            "count1.png",
            "count2.png",
            "count3.png",
            "go.png",
            "ready.png",
            // sounds
            "count1s.wav", "count1s.mp3", "count1s.ogg",
            "count2s.wav", "count2s.mp3", "count2s.ogg",
            "count3s.wav", "count3s.mp3", "count3s.ogg",
            "gos.wav", "gos.mp3", "gos.ogg",
            "readys.wav", "readys.mp3", "readys.ogg"
        ];

        private static readonly string[] skinStandardSlider =
        [
            // slider
            "sliderstartcircle.png",
            "sliderstartcircleoverlay.png",
            "sliderstartcircleoverlay-{n}.png",
            "sliderendcircle.png",
            "sliderendcircleoverlay.png",
            "sliderendcircleoverlay-{n}.png",
            "sliderfollowcircle.png",
            "sliderfollowcircle-{n}.png",
            "sliderb.png",
            "sliderb{n}.png",
            "sliderb-nd.png",
            "sliderb-spec.png",
            "sliderscorepoint.png",
            "sliderpoint10.png",
            "sliderpoint30.png"
        ];

        private static readonly string[] skinStandardSpinner =
        [
            // spinner
            "spinner-approachcircle.png",
            "spinner-rpm.png",
            "spinner-clear.png",
            "spinner-spin.png",
            "spinner-glow.png",
            "spinner-bottom.png",
            "spinner-top.png",
            "spinner-middle2.png",
            "spinner-middle.png",
            // "old spinner" but apparently it's used still without needing to be in skin v1
            "spinner-background.png",
            "spinner-circle.png",
            "spinner-metre.png",
            "spinner-osu.png",
            // sounds
            "spinnerspin.wav", "spinnerspin.mp3", "spinnerspin.ogg",
            "spinnerbonus.wav", "spinnerbonus.mp3", "spinnerbonus.ogg"
        ];

        private static readonly string[] skinNotSliderb =
        [
            "sliderb-nd.png",
            "sliderb-spec.png"
        ];

        private static readonly string[] skinBreak =
        [
            "section-fail.png",
            "section-pass.png",
            // sounds
            "sectionpass.wav", "sectionpass.mp3", "sectionpass.ogg",
            "sectionfail.wav", "sectionfail.mp3", "sectionfail.ogg"
        ];

        private static readonly List<SkinCondition> skinConditions = [];

        private static void AddElements(string[] elements, Func<BeatmapSet, bool>? useCondition = null) => skinConditions.Add(new SkinCondition(elements, useCondition));

        private static void AddElement(string element, Func<BeatmapSet, bool>? useCondition = null) => skinConditions.Add(new SkinCondition([element], useCondition));

        private static void Initialize()
        {
            // modes, doing or-gates on standard for everything because conversions
            AddElements(skinGeneral);

            AddElements(skinStandard, beatmapSet => beatmapSet.Beatmaps.Any(beatmap => beatmap.GeneralSettings.mode == Beatmap.Mode.Standard));

            AddElements(skinCatch, beatmapSet => beatmapSet.Beatmaps.Any(beatmap => beatmap.GeneralSettings.mode == Beatmap.Mode.Catch || beatmap.GeneralSettings.mode == Beatmap.Mode.Standard));

            AddElements(skinMania, beatmapSet => beatmapSet.Beatmaps.Any(beatmap => beatmap.GeneralSettings.mode == Beatmap.Mode.Mania || beatmap.GeneralSettings.mode == Beatmap.Mode.Standard));

            AddElements(skinTaiko, beatmapSet => beatmapSet.Beatmaps.Any(beatmap => beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko || beatmap.GeneralSettings.mode == Beatmap.Mode.Standard));

            AddElements(skinNotMania, beatmapSet => beatmapSet.Beatmaps.Any(beatmap => beatmap.GeneralSettings.mode != Beatmap.Mode.Mania));

            // TODO: Taiko skin conversion, see issue #6
            /*AddElements(mSkinTaiko, beatmapSet => beatmapSet.mBeatmaps.Any(
                beatmap => beatmap.mGeneralSettings.mMode == Beatmap.Mode.Taiko
                         || beatmap.mGeneralSettings.mMode == Beatmap.Mode.Standard));*/

            // only used in specific cases
            AddElements(skinCountdown, beatmapSet => beatmapSet.Beatmaps.Any(beatmap => beatmap.GeneralSettings.countdown > 0));

            AddElement("reversearrow.png", beatmapSet => beatmapSet.Beatmaps.Any(beatmap => beatmap.HitObjects.Any(hitObject => (hitObject as Slider)?.EdgeAmount > 1)));

            AddElements(skinBreak, beatmapSet => beatmapSet.Beatmaps.Any(beatmap => beatmap.Breaks.Any()));

            // sliders
            AddElements(skinStandardSlider, beatmapSet => beatmapSet.Beatmaps.Any(beatmap => beatmap.GeneralSettings.mode == Beatmap.Mode.Standard && beatmap.HitObjects.Any(hitObject => hitObject is Slider)));

            AddElements(skinTaikoSlider, beatmapSet => beatmapSet.Beatmaps.Any(beatmap => (beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko || beatmap.GeneralSettings.mode == Beatmap.Mode.Standard) && beatmap.HitObjects.Any(hitObject => hitObject is Slider)));

            AddElements(skinCatchSlider, beatmapSet => beatmapSet.Beatmaps.Any(beatmap => (beatmap.GeneralSettings.mode == Beatmap.Mode.Catch || beatmap.GeneralSettings.mode == Beatmap.Mode.Standard) && beatmap.HitObjects.Any(hitObject => hitObject is Slider)));

            // spinners
            AddElements(skinStandardSpinner, beatmapSet => beatmapSet.Beatmaps.Any(beatmap => beatmap.GeneralSettings.mode == Beatmap.Mode.Standard && beatmap.HitObjects.Any(hitObject => hitObject is Spinner)));

            AddElements(skinTaikoSpinner, beatmapSet => beatmapSet.Beatmaps.Any(beatmap => (beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko || beatmap.GeneralSettings.mode == Beatmap.Mode.Standard) && beatmap.HitObjects.Any(hitObject => hitObject is Spinner)));

            AddElements(skinCatchSpinner, beatmapSet => beatmapSet.Beatmaps.Any(beatmap => (beatmap.GeneralSettings.mode == Beatmap.Mode.Catch || beatmap.GeneralSettings.mode == Beatmap.Mode.Standard) && beatmap.HitObjects.Any(hitObject => hitObject is Spinner)));

            // depending on other skin elements
            AddElements(skinNotSliderb, beatmapSet => !beatmapSet.SongFilePaths.Any(path => PathStatic.CutPath(path) == "sliderb.png"));

            AddElement("particle50.png", beatmapSet => beatmapSet.SongFilePaths.Any(path => PathStatic.CutPath(path) == "hit50.png"));

            AddElement("particle100.png", beatmapSet => beatmapSet.SongFilePaths.Any(path => PathStatic.CutPath(path) == "hit100.png"));

            AddElement("particle300.png", beatmapSet => beatmapSet.SongFilePaths.Any(path => PathStatic.CutPath(path) == "hit300.png"));

            // animatable elements (animation takes priority over still frame)
            foreach (var skinCondition in skinConditions.ToList())
                foreach (var elementName in skinCondition.elementNames)
                    if (elementName.Contains("-{n}"))
                        AddStillFrame(elementName.Replace("-{n}", ""));

            isInitialized = true;
        }

        private static void AddStillFrame(string stillFrame)
        {
            var animatedVersion = stillFrame.Insert(stillFrame.IndexOf(".", StringComparison.Ordinal), "-{n}");

            if (skinConditions.Any(condition => condition.elementNames.Contains(animatedVersion)))
                AddElement(stillFrame, beatmapSet => !beatmapSet.SongFilePaths.Any(path => IsAnimationFrameOf(PathStatic.CutPath(path), animatedVersion)));
        }

        private static bool IsAnimationFrameOf(string elementName, string animationName)
        {
            // animationName "abc-{n}.png"
            // elementName   "abc-71.png"

            var startIndex = animationName.IndexOf("{n}", StringComparison.Ordinal);

            if (startIndex != -1 && elementName.Length > startIndex)
            {
                // Capture from where {n} is until no digits are left.
                var animationFrame = Regex.Match(elementName.Substring(startIndex), @"^\d+").Value;

                if (animationName.Replace("{n}", animationFrame).ToLower() == elementName)
                    return true;
            }

            return false;
        }

        private static SkinCondition? GetSkinCondition(string elementName)
        {
            foreach (var skinCondition in skinConditions.ToList())
                foreach (var otherElementName in skinCondition.elementNames)
                {
                    if (otherElementName.ToLower() == elementName.ToLower())
                        return skinCondition;

                    // Animation frames (i.e. "followpoint-{n}.png").
                    if (otherElementName.Contains("{n}"))
                    {
                        var startIndex = otherElementName.IndexOf("{n}", StringComparison.Ordinal);

                        if (startIndex != -1 && elementName.Length > startIndex && elementName.IndexOf('.', startIndex) != -1)
                        {
                            var endIndex = elementName.IndexOf('.', startIndex);
                            var frame = elementName.Substring(startIndex, endIndex - startIndex);

                            if (otherElementName.Replace("{n}", frame).ToLower() == elementName)
                                return skinCondition;
                        }
                    }
                }

            return null;
        }

        /// <summary> Returns whether the given skin name is used in the given beatmapset (including animations). </summary>
        public static bool IsUsed(string elementName, BeatmapSet beatmapSet)
        {
            if (!isInitialized)
                Initialize();

            // Find the respective condition for the skin element to be used.
            var skinCondition = GetSkinCondition(elementName);

            // If the condition is null, the skin element is unrecognized and as such not used.
            return skinCondition is SkinCondition condition && (condition.isUsed == null || condition.isUsed(beatmapSet));
        }

        // here we do skin elements that aren't necessarily used but can be, given a specific condition
        private struct SkinCondition
        {
            public readonly string[] elementNames;
            public readonly Func<BeatmapSet, bool> isUsed;

            public SkinCondition(string[] elementNames, Func<BeatmapSet, bool> isUsed)
            {
                this.elementNames = elementNames;
                this.isUsed = isUsed;
            }
        }
    }
}