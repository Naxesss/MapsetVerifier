using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Rendering
{
    internal abstract class TimelineRenderer : OverviewRenderer
    {
        private const int ZOOM_FACTOR = 8;
        private const int MILLISECOND_MARGIN = 2000;

        public new static string Render(BeatmapSet beatmapSet) => RenderTimelineComparison(beatmapSet);

        private static string RenderTimelineComparison(BeatmapSet beatmapSet) =>
            RenderContainer("Timeline Comparison (Prototype)",
                RenderField("Navigation",
                    RenderField("Move timeline", "Click & drag"),
                    RenderField("Move timeline faster", "Click & drag + Shift"),
                    RenderField("Move timeline slower", "Click & drag + Ctrl")),
                RenderField("Timestamps",
                    RenderField("See timestamp", "Hover"),
                    RenderField("Goto timestamp", "Alt + Left Click"),
                    RenderField("Copy timestamp", "Alt + Right Click")),
                RenderField("Hit sounds",
                    RenderField("Red lines", "Clap"),
                    RenderField("Green lines", "Finish"),
                    RenderField("Blue lines", "Whistle")),
                Div("overview-timeline-top",
                    RenderTop(beatmapSet)),
                Div("overview-timeline-content",
                    RenderContent(beatmapSet)),
                Div("overview-timeline-footer",
                    RenderFooter()));

        private static string RenderTop(BeatmapSet beatmapSet) =>
            Div("overview-timeline-difficulties",
                string.Concat(beatmapSet.Beatmaps.Select(beatmap =>
                    Div("overview-timeline-difficulty noselect",
                        beatmap.MetadataSettings.version))));

        private static string RenderContent(BeatmapSet beatmapSet)
        {
            var contentHTML = new StringBuilder();
            var startTime = GetStartTime(beatmapSet);
            var endTime = GetEndTime(beatmapSet);

            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                // Mania can have multiple notes at the same time so we'll need to do that differently.
                if (beatmap.GeneralSettings.mode == Beatmap.Mode.Mania)
                    continue;

                contentHTML.Append(
                    Div("overview-timeline locked noselect",
                        Div("overview-timeline-right",
                            Div("overview-timeline-right-options",
                                DivAttr("overview-timeline-right-option overview-timeline-right-option-remove", Tooltip("Removes the timeline.")),
                                DivAttr("overview-timeline-right-option overview-timeline-right-option-lock", Tooltip("Locks or unlocks scrolling. All locked timelines scroll together."))),
                            Div("overview-timeline-right-title",
                                beatmap.MetadataSettings.version)),
                        Div("overview-timeline-ticks",
                            RenderTicks(beatmap, startTime, endTime)),
                        Div("overview-timeline-objects",
                            RenderObjects(beatmap, startTime))));
            }

            return contentHTML.ToString();
        }

        private static double GetStartTime(BeatmapSet beatmapSet)
        {
            var startTimeObjects = beatmapSet.Beatmaps.Min(beatmap => beatmap.HitObjects.FirstOrDefault()?.time ?? 0);
            var startTimeLines = beatmapSet.Beatmaps.Min(beatmap => beatmap.TimingLines.FirstOrDefault()?.Offset ?? 0);

            return (startTimeObjects < startTimeLines ? startTimeObjects : startTimeLines) - MILLISECOND_MARGIN;
        }

        private static double GetEndTime(BeatmapSet beatmapSet)
        {
            var endTimeObjects = beatmapSet.Beatmaps.Max(beatmap => beatmap.HitObjects.LastOrDefault()?.GetEndTime() ?? 0);
            var endTimeLines = beatmapSet.Beatmaps.Max(beatmap => beatmap.TimingLines.LastOrDefault()?.Offset ?? 0);

            return (endTimeObjects < endTimeLines ? endTimeObjects : endTimeLines) + MILLISECOND_MARGIN;
        }

        private static string RenderFooter() =>
            Div("overview-timeline-slider") +
            Div("overview-timeline-buttons",
                DivAttr("overview-timeline-button plus-icon", Tooltip("Zooms in timelines.")),
                DivAttr("overview-timeline-button minus-icon", Tooltip("Zooms out timelines.")),
                Div("overview-timeline-buttons-zoomamount",
                    "1×"));

        private static string RenderTicks(Beatmap beatmap, double startTime, double endTime)
        {
            double sampleTime;
            var prevSampleTime = startTime;

            return string.Concat(beatmap.TimingLines.OfType<UninheritedLine>().Select(line =>
            {
                var nextLine = beatmap.GetNextTimingLine<UninheritedLine>(line.Offset);
                var nextSwap = nextLine?.Offset ?? endTime;

                var tickDivs = new StringBuilder();

                // To get precision down to both 1/16th and 1/12th of a beat we need to sample...
                // 16 = 2^4, 12 = 2^2*3, 2^4*3 = 48 times per beat.
                // We're going to intentionally ignore 1/5, 1/7, and 1/9, as we'd be sampling way too much.
                var samplesPerBeat = 48;

                for (var i = 0; i < (nextSwap - line.Offset) / line.msPerBeat * samplesPerBeat; ++i)
                {
                    // Add the practical unsnap to avoid things getting unsnapped the further into the map you go.
                    sampleTime = line.Offset + i * line.msPerBeat / samplesPerBeat + beatmap.GetPracticalUnsnap(line.Offset + i * line.msPerBeat / samplesPerBeat);

                    var hasEdge = beatmap.GetHitObject(sampleTime)?.GetEdgeTimes().Any(edgeTime => Math.Abs(edgeTime - sampleTime) < 2) ?? false;

                    if (i % (samplesPerBeat / 4) != 0 && (!hasEdge || (i % (samplesPerBeat / 12) != 0 && i % (samplesPerBeat / 16) != 0)))
                        continue;

                    tickDivs.Append(
                        DivAttr("overview-timeline-tick", " style=\"margin-left:" + (sampleTime - prevSampleTime) / ZOOM_FACTOR + "px\"",
                            Div("overview-timeline-ticks-base " + (hasEdge ? " hasobject " : "") + (i % (samplesPerBeat * line.Meter) == 0
                                ? "overview-timeline-ticks-largewhite"
                                : (
                                    i % (samplesPerBeat * 1) == 0 ? "overview-timeline-ticks-white" :
                                    i % (samplesPerBeat / 2) == 0 ? "overview-timeline-ticks-red" :
                                    i % (samplesPerBeat / 3) == 0 ? "overview-timeline-ticks-magenta" :
                                    i % (samplesPerBeat / 4) == 0 ? "overview-timeline-ticks-blue" :
                                    i % (samplesPerBeat / 6) == 0 ? "overview-timeline-ticks-purple" :
                                    i % (samplesPerBeat / 8) == 0 ? "overview-timeline-ticks-yellow" :
                                    i % (samplesPerBeat / 12) == 0 ? "overview-timeline-ticks-gray" :
                                    i % (samplesPerBeat / 16) == 0 ? "overview-timeline-ticks-gray" : "overview-timeline-ticks-unsnapped"
                                )))));

                    prevSampleTime = sampleTime;
                }

                return tickDivs.ToString();
            }));
        }

        private static string RenderObjects(Beatmap beatmap, double startTime)
        {
            var objectTime = startTime;

            return string.Concat(beatmap.HitObjects.Select(hitObject =>
            {
                var prevTime = objectTime;
                objectTime = hitObject.time;

                switch (hitObject)
                {
                    case Circle circle:
                        return
                            DivAttr("overview-timeline-object", " style=\"margin-left:" + (circle.time - prevTime) / ZOOM_FACTOR + "px;\"",
                                DivAttr("overview-timeline-circle", DataAttr("timestamp", Timestamp.Get(circle.time)) + DataAttr("tooltip", Timestamp.Get(circle.time)) + " style=\"" + RenderHitObjectBackgroundStyle(circle, beatmap) + RenderHitObjectSizeStyle(circle, beatmap) + "\"",
                                    RenderHitObjectHitSound(circle, beatmap)));

                    case Slider slider:
                    {
                        // Big drumrolls need additional length due to the increased radius.
                        // In taiko regular notes are smaller so it needs a reduced radius otherwise.
                        var addedWidth = beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko ? slider.HasHitSound(HitObject.HitSounds.Finish) ? 32 : 22 : 23;

                        return
                            DivAttr("overview-timeline-object", DataAttr("timestamp", Timestamp.Get(slider.time)) + DataAttr("tooltip", Timestamp.Get(slider.time)) + " style=\"margin-left:" + (slider.time - prevTime) / ZOOM_FACTOR + "px;\"",
                                string.Concat(slider.GetEdgeTimes().Select((edgeTime, index) =>
                                    DivAttr("overview-timeline-object edge", "style=\"margin-left:" + (edgeTime - slider.time) / ZOOM_FACTOR + "px;\"",
                                        DivAttr("overview-timeline-edge" + (index > 0 && index < slider.EdgeAmount ? " overview-timeline-edge-reverse" : ""), " style=\"" + RenderHitObjectSizeStyle(slider, beatmap) + "\"",
                                            RenderHitObjectHitSound(slider, beatmap, index))))),
                                DivAttr("overview-timeline-path", " style=\"" + "width:" + (slider.EndTime - slider.time) / ZOOM_FACTOR + "px;" + "padding-right:" + addedWidth + "px;" + RenderHitObjectBackgroundStyle(slider, beatmap) + RenderHitObjectSizeStyle(slider, beatmap, true) + "\""));
                    }

                    case Spinner spinner:
                    {
                        var addedWidth = beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko ? 22 : 23;

                        return
                            DivAttr("overview-timeline-object", DataAttr("timestamp", Timestamp.Get(spinner.time)) + DataAttr("tooltip", Timestamp.Get(spinner.time)) + " style=\"margin-left:" + (spinner.time - prevTime) / ZOOM_FACTOR + "px;\"",
                                Div("overview-timeline-object edge",
                                    Div("overview-timeline-edge",
                                        RenderHitObjectHitSound(spinner, beatmap))),
                                DivAttr("overview-timeline-object edge", " style=\"margin-left:" + (spinner.endTime - spinner.time) / ZOOM_FACTOR + "px;\"",
                                    Div("overview-timeline-edge",
                                        RenderHitObjectHitSound(spinner, beatmap, 1))),
                                DivAttr("overview-timeline-path", " style=\"" + "width:" + (spinner.endTime - spinner.time) / ZOOM_FACTOR + "px;" + "padding-right:" + addedWidth + "px;\""));
                    }

                    default:
                        return DivAttr("overview-timeline-object", " style=\"margin-left:" + (hitObject.time - prevTime) / ZOOM_FACTOR + "px;\"");
                }
            }));
        }

        private static string RenderHitObjectBackgroundStyle(HitObject hitObject, Beatmap beatmap)
        {
            if (beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko)
            {
                // drumroll
                if (hitObject is Slider)
                    return "background-color:rgba(252,191,31,0.5);";

                // kat
                if (hitObject.HasHitSound(HitObject.HitSounds.Clap) || hitObject.HasHitSound(HitObject.HitSounds.Whistle))
                    return "background-color:rgba(68,141,171,0.5);";

                // don
                return "background-color:rgba(235,69,44,0.5);";
            }

            var colourIndex = beatmap.GetComboColourIndex(hitObject.time);

            if (beatmap.ColourSettings.combos.Count() > colourIndex)
                return "background-color:rgba(" + beatmap.ColourSettings.combos[colourIndex].X + "," + beatmap.ColourSettings.combos[colourIndex].Y + "," + beatmap.ColourSettings.combos[colourIndex].Z + ", 0.5);";

            // Should no custom combo colours exist, objects will simply be gray.
            return "background-color:rgba(125,125,125, 0.5);";
        }

        private static string RenderHitObjectSizeStyle(HitObject hitObject, Beatmap beatmap, bool isSliderPath = false)
        {
            if (beatmap.GeneralSettings.mode != Beatmap.Mode.Taiko)
                return "";

            if (hitObject.HasHitSound(HitObject.HitSounds.Finish))
                // big don/kat
                return "height:30px;" + (isSliderPath ? "border-radius:15px;" : "width:30px;") + "margin-left:-15.5px;";

            return "height:20px;" + (isSliderPath ? "border-radius:10px;" : "width:20px;") + "margin-left:-10.5px;" + "margin-bottom:-2px;";

        }

        private static string RenderHitObjectHitSound(HitObject hitObject, Beatmap beatmap, int edgeIndex = 0)
        {
            if (beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko)
                // Taiko modifies gameplay through hit sounds, and this gameplay effect is visible in the timeline already.
                return "";

            HitObject.HitSounds? hitSound;

            if (edgeIndex == 0)
                hitSound = hitObject.GetStartHitSound();
            else if (hitObject is Slider slider && slider.ReverseHitSounds.Count > edgeIndex - 1)
                hitSound = slider.ReverseHitSounds[edgeIndex - 1];
            else
                hitSound = hitObject.GetEndHitSound();

            var styles = new List<string> { "overview-timeline-hs" };

            if (!(hitSound is HitObject.HitSounds hs))
                return "";

            if (hs.HasFlag(HitObject.HitSounds.Whistle)) styles.Add("overview-timeline-hs-whistle");
            if (hs.HasFlag(HitObject.HitSounds.Clap)) styles.Add("overview-timeline-hs-clap");
            if (hs.HasFlag(HitObject.HitSounds.Finish)) styles.Add("overview-timeline-hs-finish");

            return Div(string.Join(" ", styles));
        }
    }
}