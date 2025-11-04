using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Objects.TimingLines;
using MapsetVerifier.Parser.Settings;
using MapsetVerifier.Parser.Statics;
using MathNet.Numerics;

namespace MapsetVerifier.Rendering
{
    public class OverviewRenderer : BeatmapInfoRenderer
    {
        public static string Render(BeatmapSet beatmapSet) =>
            string.Concat(
                RenderBeatmapInfo(beatmapSet),
                Div("paste-separator"),
                RenderTimelineComparison(beatmapSet),
                RenderSkillCharts(beatmapSet),
                RenderSnappings(beatmapSet),
                RenderMetadata(beatmapSet),
                RenderGeneralSettings(beatmapSet),
                RenderDifficultySettings(beatmapSet),
                RenderStatistics(beatmapSet),
                RenderResources(beatmapSet),
                RenderColourSettings(beatmapSet),
                Div("overview-footer"));

        private static string RenderTimelineComparison(BeatmapSet beatmapSet) => TimelineRenderer.Render(beatmapSet);

        private static string RenderSkillCharts(BeatmapSet beatmapSet) => SkillChartRenderer.Render(beatmapSet);

        private static string RenderSnappings(BeatmapSet beatmapSet)
        {
            var divisorStamps = new Dictionary<Beatmap, Dictionary<int, List<string>>>();
            var divisors = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 16 };

            var parts = new List<string>
            {
                "Circle",
                "Slider head", "Slider tail", "Slider reverse",
                "Spinner head", "Spinner tail",
                "Hold note head", "Hold note tail"
            };

            foreach (var beatmap in beatmapSet.Beatmaps)
            {
                if (divisorStamps.GetValueOrDefault(beatmap) == null)
                    divisorStamps[beatmap] = new Dictionary<int, List<string>>();

                foreach (var divisor in divisors)
                    if (divisorStamps[beatmap].GetValueOrDefault(divisor) == null)
                        divisorStamps[beatmap][divisor] = new List<string>();

                foreach (var hitObject in beatmap.HitObjects)
                    foreach (var edgeTime in hitObject.GetEdgeTimes())
                    {
                        var divisor = beatmap.GetLowestDivisor(edgeTime);

                        if (!divisors.Contains(divisor))
                            continue;

                        var stamp = Timestamp.Get(edgeTime) + $"({hitObject.GetPartName(edgeTime)})";

                        divisorStamps[beatmap][divisor].Add(stamp);
                    }
            }

            return RenderContainer("Snappings", 
                beatmapSet.Beatmaps.Select(beatmap =>
                    RenderField(beatmap.MetadataSettings.version,
                        divisors.Select(divisor =>
                            RenderClosedField($"1/{divisor} ({divisorStamps[beatmap][divisor].Count()})",
                                divisorStamps[beatmap][divisor].Any()
                                    ? string.Join("", parts.Select(part =>
                                        RenderField($"{part}s ({divisorStamps[beatmap][divisor].Count(stamp => stamp.Contains(part))})",
                                            FormatTimestamps(divisorStamps[beatmap][divisor].Any(stamp => stamp.Contains(part))
                                                ? string.Join("<br>", divisorStamps[beatmap][divisor].Where(stamp => stamp.Contains(part)))
                                                : "N/A"))).ToArray())
                                    : "N/A")).ToArray())).ToArray());
        }

        private static string RenderMetadata(BeatmapSet beatmapSet) =>
            // If the romanised field is always the same as the unicode field, we don't need to display them separately.
            RenderContainer("Metadata",
                beatmapSet.Beatmaps.Any(beatmap => beatmap.MetadataSettings.artist?.ToString() != beatmap.MetadataSettings.artistUnicode?.ToString())
                ? RenderField("Artist",
                    RenderBeatmapContent(beatmapSet,
                        "Romanised", 
                        beatmap => beatmap.MetadataSettings.artist?.ToString()) +
                    RenderBeatmapContent(beatmapSet,
                        "Unicode", 
                        beatmap => beatmap.MetadataSettings.artistUnicode?.ToString()))
                : RenderBeatmapContent(beatmapSet, 
                    "Artist", 
                    beatmap => beatmap.MetadataSettings.artist?.ToString()),
                beatmapSet.Beatmaps.Any(beatmap => beatmap.MetadataSettings.title?.ToString() != beatmap.MetadataSettings.titleUnicode?.ToString())
                    ? RenderField("Title",
                        RenderBeatmapContent(beatmapSet, 
                            "Romanised", 
                            beatmap => beatmap.MetadataSettings.title?.ToString()) +
                        RenderBeatmapContent(beatmapSet, 
                            "Unicode", 
                            beatmap => beatmap.MetadataSettings.titleUnicode?.ToString()))
                    : RenderBeatmapContent(beatmapSet, 
                        "Title", 
                        beatmap => beatmap.MetadataSettings.title?.ToString()),
                RenderBeatmapContent(beatmapSet, 
                    "Creator", 
                    beatmap => beatmap.MetadataSettings.creator?.ToString()),
                RenderBeatmapContent(beatmapSet, 
                    "Source", 
                    beatmap => beatmap.MetadataSettings.source?.ToString()),
                RenderBeatmapContent(beatmapSet, 
                    "Tags", 
                    beatmap => beatmap.MetadataSettings.tags?.ToString()));

        private static string RenderGeneralSettings(BeatmapSet beatmapSet) =>
            RenderContainer("General Settings",
                RenderBeatmapContent(beatmapSet, 
                    "Audio Filename", 
                    beatmap => beatmap.GeneralSettings.audioFileName?.ToString()),
                RenderBeatmapContent(beatmapSet, 
                    "Audio Lead-in", 
                    beatmap => beatmap.GeneralSettings.audioLeadIn.ToString(CultureInfo.InvariantCulture)),
                RenderBeatmapContent(beatmapSet, 
                    "Mode", 
                    beatmap => beatmap.GeneralSettings.mode.ToString()),
                RenderBeatmapContent(beatmapSet, 
                    "Stack Leniency", 
                    beatmap =>
                    {
                        // Stack leniency only does stuff for standard.
                        if (beatmap.GeneralSettings.mode == Beatmap.Mode.Standard)
                            return beatmap.GeneralSettings.stackLeniency.ToString(CultureInfo.InvariantCulture);

                        return "N/A";
                    }),
                RenderField("Countdown Settings",
                    RenderBeatmapContent(beatmapSet,
                        "Has Countdown",
                        beatmap => (beatmap.GetCountdownStartBeat() >= 0 && beatmap.GeneralSettings.countdown != GeneralSettings.Countdown.None).ToString()),
                    RenderBeatmapContent(beatmapSet,
                        "Countdown Speed",
                        beatmap =>
                        {
                            if (beatmap.GetCountdownStartBeat() >= 0 && beatmap.GeneralSettings.countdown != GeneralSettings.Countdown.None)
                                return beatmap.GeneralSettings.countdown.ToString();

                            return "N/A";
                        }), 
                    RenderBeatmapContent(beatmapSet,
                        "Countdown Offset",
                        beatmap =>
                        {
                            if (beatmap.GetCountdownStartBeat() >= 0 && beatmap.GeneralSettings.countdown != GeneralSettings.Countdown.None)
                                return beatmap.GeneralSettings.countdownBeatOffset.ToString();

                            return "N/A";
                        })),
                RenderBeatmapContent(beatmapSet,
                    "Epilepsy Warning",
                    beatmap =>
                    {
                        if (beatmap.Videos.Any() || beatmap.HasDifficultySpecificStoryboard() || (beatmapSet.Osb?.IsUsed() ?? false))
                            return beatmap.GeneralSettings.epilepsyWarning.ToString();

                        return "N/A";
                    }),
                RenderBeatmapContent(beatmapSet,
                    "Letterbox During Breaks",
                    beatmap =>
                    {
                        if (beatmap.Breaks.Any() || !beatmap.GeneralSettings.letterbox)
                            return beatmap.GeneralSettings.letterbox.ToString();

                        return "N/A";
                    }),
                RenderField("Storyboard Settings",
                    RenderBeatmapContent(beatmapSet,
                        "Has Storyboard",
                        beatmap => (beatmap.HasDifficultySpecificStoryboard() || (beatmapSet.Osb?.IsUsed() ?? false)).ToString()),
                    RenderBeatmapContent(beatmapSet,
                        "Widescreen Support",
                        beatmap =>
                        {
                            if (beatmap.HasDifficultySpecificStoryboard() || (beatmapSet.Osb?.IsUsed() ?? false))
                                return beatmap.GeneralSettings.widescreenSupport.ToString();

                            return "N/A";
                        }),
                    RenderBeatmapContent(beatmapSet,
                        "In Front Of Combo Fire",
                        beatmap =>
                        {
                            if (beatmap.HasDifficultySpecificStoryboard() || (beatmapSet.Osb?.IsUsed() ?? false))
                                return beatmap.GeneralSettings.storyInFrontOfFire.ToString();

                            return "N/A";
                        }),
                    RenderBeatmapContent(beatmapSet,
                        "Use Skin Sprites",
                        beatmap =>
                        {
                            if (beatmap.HasDifficultySpecificStoryboard() || (beatmapSet.Osb?.IsUsed() ?? false))
                                return beatmap.GeneralSettings.useSkinSprites.ToString();

                            return "N/A";
                        })),
                RenderBeatmapContent(beatmapSet,
                    "Preview Time",
                    beatmap => beatmap.GeneralSettings.previewTime >= 0
                        ? Timestamp.Get(beatmap.GeneralSettings.previewTime)
                        : ""),
                RenderBeatmapContent(beatmapSet,
                    "Skin Preference",
                    beatmap => beatmap.GeneralSettings.skinPreference?.ToString())

                // Special N+1 Style is apparently not used by any mode, was meant for mania but was later overriden by user settings.
            );

        private static string RenderDifficultySettings(BeatmapSet beatmapSet) =>
            RenderContainer("Difficulty Settings",
                RenderBeatmapContent(beatmapSet,
                    "HP Drain",
                    beatmap => beatmap.DifficultySettings.hpDrain.ToString(CultureInfo.InvariantCulture)),
                RenderBeatmapContent(beatmapSet, "Circle Size", beatmap =>
                    {
                        if (beatmap.GeneralSettings.mode != Beatmap.Mode.Taiko)
                            return beatmap.DifficultySettings.circleSize.ToString(CultureInfo.InvariantCulture);

                        return "N/A";
                    }),
                RenderBeatmapContent(beatmapSet, "Overall Difficulty", beatmap =>
                    beatmap.DifficultySettings.overallDifficulty.ToString(CultureInfo.InvariantCulture)),
                RenderBeatmapContent(beatmapSet, "Approach Rate", beatmap =>
                    {
                        if (beatmap.GeneralSettings.mode != Beatmap.Mode.Mania)
                            return beatmap.DifficultySettings.approachRate.ToString(CultureInfo.InvariantCulture);

                        return "N/A";
                    }),
                RenderBeatmapContent(beatmapSet, "Slider Tick Rate", beatmap =>
                    {
                        if (beatmap.GeneralSettings.mode != Beatmap.Mode.Mania)
                            return beatmap.DifficultySettings.sliderTickRate.ToString(CultureInfo.InvariantCulture);

                        return "N/A";
                    }),
                RenderBeatmapContent(beatmapSet, "SV Multiplier", beatmap =>
                    {
                        if (beatmap.GeneralSettings.mode != Beatmap.Mode.Mania)
                            return beatmap.DifficultySettings.sliderMultiplier.ToString(CultureInfo.InvariantCulture);

                        return "N/A";
                    }));

        private static string RenderStatistics(BeatmapSet beatmapSet)
        {
            return
                RenderContainer("Statistics",
                    RenderBeatmapContent(beatmapSet, "Approx Star Rating", beatmap =>
                        {
                            // Current star rating calc only supports standard and taiko.
                            if (beatmap.GeneralSettings.mode == Beatmap.Mode.Standard || beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko)
                                return $"{beatmap.StarRating:0.##}";

                            return "N/A";
                        }),
                    RenderBeatmapContent(beatmapSet, "Circles", beatmap =>
                        beatmap.HitObjects.OfType<Circle>().Count().ToString(CultureInfo.InvariantCulture)),
                    RenderBeatmapContent(beatmapSet, "Sliders", beatmap =>
                        {
                            // Sliders don't exist in mania.
                            if (beatmap.GeneralSettings.mode == Beatmap.Mode.Mania)
                                return "N/A";

                            return beatmap.HitObjects.OfType<Slider>().Count().ToString(CultureInfo.InvariantCulture);
                        }),
                    RenderBeatmapContent(beatmapSet, "Spinners", beatmap =>
                        {
                            // Spinners don't exist in mania.
                            if (beatmap.GeneralSettings.mode != Beatmap.Mode.Mania)
                                return beatmap.HitObjects.OfType<Spinner>().Count().ToString(CultureInfo.InvariantCulture);

                            return "N/A";
                        }),
                    RenderBeatmapContent(beatmapSet, "Hold Notes", beatmap =>
                        {
                            // Hold notes only exist in mania.
                            if (beatmap.GeneralSettings.mode == Beatmap.Mode.Mania)
                                return beatmap.HitObjects.OfType<HoldNote>().Count().ToString(CultureInfo.InvariantCulture);

                            return "N/A";
                        }),
                    RenderBeatmapContent(beatmapSet, "Objects per Column", beatmap =>
                        {
                            // Columns only exist in mania.
                            if (beatmap.GeneralSettings.mode != Beatmap.Mode.Mania)
                                return "N/A";

                            var column1 = beatmap.HitObjects.Count(hitObject => hitObject.Position.X.AlmostEqual(64));
                            var column2 = beatmap.HitObjects.Count(hitObject => hitObject.Position.X.AlmostEqual(192));
                            var column3 = beatmap.HitObjects.Count(hitObject => hitObject.Position.X.AlmostEqual(320));
                            var column4 = beatmap.HitObjects.Count(hitObject => hitObject.Position.X.AlmostEqual(448));

                            return $"{column1}|{column2}|{column3}|{column4}";

                        }),
                    RenderBeatmapContent(beatmapSet, "New Combos", beatmap => beatmap.HitObjects.Count(@object => @object.type.HasFlag(HitObject.Types.NewCombo)).ToString(CultureInfo.InvariantCulture)), 
                    RenderBeatmapContent(beatmapSet, "Breaks", beatmap => beatmap.Breaks.Count.ToString(CultureInfo.InvariantCulture)),
                    RenderBeatmapContent(beatmapSet, "Uninherited Lines", beatmap => beatmap.TimingLines.OfType<UninheritedLine>().Count().ToString(CultureInfo.InvariantCulture)),
                    RenderBeatmapContent(beatmapSet, "Inherited Lines", beatmap => beatmap.TimingLines.OfType<InheritedLine>().Count().ToString(CultureInfo.InvariantCulture)),
                    RenderBeatmapContent(beatmapSet, "Kiai Time", GetKiaiTime),
                    RenderBeatmapContent(beatmapSet, "Drain Time", beatmap => Timestamp.Get(beatmap.GetDrainTime(beatmap.GeneralSettings.mode)).ToString(CultureInfo.InvariantCulture)),
                    RenderBeatmapContent(beatmapSet, "Play Time", beatmap => Timestamp.Get(beatmap.GetPlayTime()).ToString(CultureInfo.InvariantCulture)));
        }

        private static string GetKiaiTime(Beatmap beatmap)
        {
            var result = beatmap.TimingLines
                .GroupBy(l => l.Offset) // collapse identical offsets
                .Select(g => g.Any(l => l.Kiai)) // whether Kiai is active at this offset
                .Zip(beatmap.TimingLines
                        .Select(l => l.Offset)
                        .Distinct()
                        .OrderBy(o => o)
                        .ToList(),
                    (isKiai, offset) => new { isKiai, offset })
                .Where(x => x.isKiai)
                .Sum(x =>
                {
                    var next = beatmap.TimingLines
                        .Where(l => l.Offset > x.offset)
                        .Select(l => l.Offset)
                        .DefaultIfEmpty(beatmap.HitObjects.First().time + beatmap.GetPlayTime())
                        .First();
                    return next - x.offset;
                });
            
            return result.ToString(CultureInfo.InvariantCulture);
        }

        private static string RenderDataSize(long size)
        {
            if (size / Math.Pow(1024, 3) >= 1)
                return $"{size / Math.Pow(1024, 3):0.##} GB";

            if (size / Math.Pow(1024, 2) >= 1 && size / Math.Pow(1024, 3) < 1)
                return $"{size / Math.Pow(1024, 2):0.##} MB";

            if (size / Math.Pow(1024, 1) >= 1 && size / Math.Pow(1024, 2) < 1)
                return $"{size / Math.Pow(1024, 1):0.##} KB";

            return $"{size / Math.Pow(1024, 0):0.##} B";
        }

        private static string RenderFileSize(string fullPath)
        {
            if (!File.Exists(fullPath))
                return "";

            var fileInfo = new FileInfo(fullPath);

            return RenderDataSize(fileInfo.Length);
        }

        private static long GetDirectorySize(DirectoryInfo directoryInfo)
        {
            long totalSize = 0;

            var fileInfos = directoryInfo.GetFiles();

            foreach (var fileInfo in fileInfos)
                totalSize += fileInfo.Length;

            var subdirectoryInfos = directoryInfo.GetDirectories();

            foreach (var subdirectoryInfo in subdirectoryInfos)
                totalSize += GetDirectorySize(subdirectoryInfo);

            return totalSize;
        }

        private static string RenderDirectorySize(string fullPath)
        {
            var directoryInfo = new DirectoryInfo(fullPath);

            return RenderDataSize(GetDirectorySize(directoryInfo));
        }

        private static string RenderResources(BeatmapSet beatmapSet)
        {
            string RenderFloat(List<string> files, Func<string, string> func)
            {
                var content = string.Join("<br>", files.Select(file =>
                {
                    var path = beatmapSet.HitSoundFiles.FirstOrDefault(otherFile => otherFile.StartsWith(file + "."));

                    return path == null ? null : func(path);
                }).Where(value => value != null));

                return content.Length == 0 ? "" : Div("overview-float", content);
            }

            var hsUsedCount = new Dictionary<string, int>();

            return RenderContainer("Resources",
                RenderBeatmapContent(beatmapSet, "Used Hit Sound File(s)", beatmap =>
                {
                    var usedHitSoundFiles = beatmap.HitObjects.SelectMany(@object => @object.GetUsedHitSoundFileNames()).ToList();

                    var distinctSortedFiles = usedHitSoundFiles.Distinct().OrderByDescending(file => file).ToList();

                    return
                        RenderFloat(distinctSortedFiles, Encode) +
                        RenderFloat(distinctSortedFiles, path =>
                        {
                            var count = usedHitSoundFiles.Count(otherFile => path.StartsWith(otherFile + "."));

                            // Used for total hit sound usage overview
                            if (!hsUsedCount.TryAdd(path, count))
                                hsUsedCount[path] += count;

                            return $"× {count}";
                        });
                }, false),
                RenderField("Total Used Hit Sound File(s)",
                    hsUsedCount.Any()
                        ? Div("overview-float",
                            string.Join("<br>", hsUsedCount.Select(pair => pair.Key))) + 
                          Div("overview-float",
                            string.Join("<br>", hsUsedCount.Select(pair => Try(() =>
                            {
                                var fullPath = Path.Combine(beatmapSet.SongPath, pair.Key);

                                return Encode(RenderFileSize(fullPath));
                            }, "Could not get hit sound file size")))) +
                          Div("overview-float", 
                            string.Join("<br>", hsUsedCount.Select(pair => Try(() =>
                            {
                                var fullPath = Path.Combine(beatmapSet.SongPath, pair.Key);
                                var duration = AudioBASS.GetDuration(fullPath);

                                return duration < 0 ? "0 ms" : $"{duration:0.##} ms";
                            }, "Could not get hit sound duration")))) +
                          Div("overview-float",
                            string.Join("<br>", hsUsedCount.Select(pair => Try(() =>
                            {
                                var fullPath = Path.Combine(beatmapSet.SongPath, pair.Key);

                                return Encode(AudioBASS.EnumToString(AudioBASS.GetFormat(fullPath)));
                            }, "Could not get hit sound file path")))) +
                          Div("overview-float",
                            string.Join("<br>", hsUsedCount.Select(pair => "× " + pair.Value)))
                        : ""),
                RenderBeatmapContent(beatmapSet, "Background File(s)", beatmap =>
                {
                    if (!beatmap.Backgrounds.Any())
                        return "";

                    var fullPath = Path.Combine(beatmap.SongPath, beatmap.Backgrounds.First().path);

                    if (!File.Exists(fullPath))
                        return "";

                    return
                        Div("overview-float",
                            Try(() => Encode(beatmap.Backgrounds.First().path),
                                "Could not get background file path")) +
                        Div("overview-float",
                            Try(() => Encode(RenderFileSize(fullPath)),
                                "Could not get background file size")) +
                        Div("overview-float",
                            Try(() =>
                                {
                                    var tagFile = new FileAbstraction(fullPath).GetTagFile();

                                    return Encode(tagFile.Properties.PhotoWidth + " \u00d7 " + tagFile.Properties.PhotoHeight);
                                },
                                "Could not get background resolution"));
                }, false),
                RenderBeatmapContent(beatmapSet, "Video File(s)", beatmap =>
                {
                    if (!beatmap.Videos.Any() && !(beatmapSet.Osb?.videos.Any() ?? false))
                        return "";

                    var fullPath = Path.Combine(beatmap.SongPath, beatmap.Videos.First().path);

                    if (!File.Exists(fullPath))
                        return "";

                    return
                        Div("overview-float",
                            Try(() => Encode(beatmap.Videos.First().path),
                                "Could not get video file path")) +
                        Div("overview-float",
                            Try(() => Encode(RenderFileSize(fullPath)),
                                "Could not get video file size")) +
                        Div("overview-float",
                            Try(() =>
                                {
                                    var tagFile = new FileAbstraction(fullPath).GetTagFile();

                                    return FormatTimestamps(Encode(Timestamp.Get(tagFile.Properties.Duration.TotalMilliseconds)));
                                },
                                "Could not get video duration")) +
                        Div("overview-float",
                            Try(() =>
                                {
                                    var tagFile = new FileAbstraction(fullPath).GetTagFile();
                                    
                                    return Encode(tagFile.Properties.VideoWidth + " \u00d7 " + tagFile.Properties.VideoHeight);
                                },
                                "Could not get video resolution"));

                }, false),
                RenderBeatmapContent(beatmapSet, "Audio File(s)", beatmap =>
                {
                    var path = beatmap.GetAudioFilePath();

                    if (path == null)
                        return "";

                    return
                        Div("overview-float",
                            Try(() => Encode(PathStatic.RelativePath(path, beatmap.SongPath)),
                                "Could not get audio file path")) +
                        Div("overview-float",
                            Try(() => Encode(RenderFileSize(path)),
                                "Could not get audio file size")) +
                        Div("overview-float",
                            Try(() => FormatTimestamps(Encode(Timestamp.Get(AudioBASS.GetDuration(path)))),
                                "Could not get audio duration")) +
                        Div("overview-float",
                            Try(() => Encode(AudioBASS.EnumToString(AudioBASS.GetFormat(path))),
                                "Could not get audio format"));
                }, false),
                RenderBeatmapContent(beatmapSet, "Audio Bitrate", beatmap =>
                {
                    var path = beatmap.GetAudioFilePath();

                    if (path == null)
                        return "N/A";

                    return
                        Div("overview-float",
                            $"average {Math.Round(AudioBASS.GetBitrate(path))} kbps");
                }, false),
                RenderField("Has .osb",
                    Encode((beatmapSet.Osb?.IsUsed() ?? false).ToString())),
                RenderBeatmapContent(beatmapSet, "Has .osu Specific Storyboard", beatmap =>
                    beatmap.HasDifficultySpecificStoryboard().ToString()),
                RenderBeatmapContent(beatmapSet, "Song Folder Size", beatmap =>
                    RenderDirectorySize(beatmap.SongPath)));
        }

        private static string Try(Func<string> func, string noteIfError = "")
        {
            try
            {
                return func();
            }
            catch (Exception exception)
            {
                return $"<span style=\"color: var(--exception);\">{noteIfError}; {exception.Message}</span>";
            }
        }

        private static float GetLuminosity(Vector3 colour) =>
            // HSP colour model http://alienryderflex.com/hsp.html
            (float)Math.Sqrt(colour.X * colour.X * 0.299f + colour.Y * colour.Y * 0.587f + colour.Z * colour.Z * 0.114f);

        private static string RenderColourSettings(BeatmapSet beatmapSet)
        {
            var maxComboAmount = beatmapSet.Beatmaps.Max(beatmap => beatmap.ColourSettings.combos.Count);
            var content = new StringBuilder();

            // Combo Colours
            for (var i = 0; i < maxComboAmount; ++i)
                content.Append(
                    RenderBeatmapContent(beatmapSet, $"Combo {i + 1}", beatmap =>
                    {
                        if (beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko || beatmap.GeneralSettings.mode == Beatmap.Mode.Mania)
                            return "N/A";

                        Vector3? comboColour = null;

                        // Need to account for that the colour at index 0 is actually the last displayed index in-game.
                        if (beatmap.ColourSettings.combos.Count > i + 1)
                            comboColour = beatmap.ColourSettings.combos[i + 1];
                        else if (beatmap.ColourSettings.combos.Count == i + 1)
                            comboColour = beatmap.ColourSettings.combos[0];

                        if (comboColour == null)
                            return DivAttr("overview-colour", DataAttr("colour", ""));

                        return DivAttr("overview-colour", DataAttr("colour", comboColour?.X + "," + comboColour?.Y + "," + comboColour?.Z) + Tooltip($"HSP luminosity {GetLuminosity(comboColour.GetValueOrDefault()):0.#}, less than 43 or greater than 250 in kiai is bad."));
                    }, false));

            // Border + Track
            content.Append(
                RenderBeatmapContent(beatmapSet, "Slider Border", beatmap =>
                {
                    if (beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko || beatmap.GeneralSettings.mode == Beatmap.Mode.Mania)
                        return "N/A";

                    var comboColour = beatmap.ColourSettings.sliderBorder;

                    if (comboColour == null)
                        return DivAttr("overview-colour", DataAttr("colour", ""));

                    return DivAttr("overview-colour", DataAttr("colour", comboColour?.X + "," + comboColour?.Y + "," + comboColour?.Z) + Tooltip($"HSP luminosity {GetLuminosity(comboColour.GetValueOrDefault()):0.#}, less than 43 is bad."));
                }, false) +
                RenderBeatmapContent(beatmapSet, "Slider Track", beatmap =>
                {
                    if (beatmap.GeneralSettings.mode == Beatmap.Mode.Taiko || beatmap.GeneralSettings.mode == Beatmap.Mode.Mania)
                        return "N/A";

                    var comboColour = beatmap.ColourSettings.sliderTrackOverride;

                    if (comboColour == null)
                        return DivAttr("overview-colour", DataAttr("colour", ""));

                    return DivAttr("overview-colour", DataAttr("colour", comboColour?.X + "," + comboColour?.Y + "," + comboColour?.Z) + Tooltip($"HSP luminosity {GetLuminosity(comboColour.GetValueOrDefault()):0.#}"));
                }, false));

            return RenderContainer("Colour Settings", content.ToString());
        }

        // Returns a single field if all values are equal, otherwise multiple.
        private static string RenderBeatmapContent(BeatmapSet beatmapSet, string title, Func<Beatmap, string> func, bool encode = true)
        {
            var beatmapContent = new Dictionary<Beatmap, string>();

            foreach (var beatmap in beatmapSet.Beatmaps)
                try
                {
                    beatmapContent[beatmap] = func(beatmap);
                }
                catch (Exception exception)
                {
                    beatmapContent[beatmap] = $"<span style=\"color: var(--exception);\">{exception.Message}</span>";
                }

            if (beatmapContent.Any(pair => pair.Value != beatmapContent.First().Value))
                return
                    RenderField(title,
                        string.Concat(beatmapSet.Beatmaps.Select(beatmap =>
                            RenderField(beatmap.MetadataSettings.version,
                                encode
                                    ? FormatTimestamps(Encode(beatmapContent[beatmap]))
                                    : beatmapContent[beatmap]))));

            return RenderField(title,
                encode
                    ? FormatTimestamps(Encode(beatmapContent.First().Value))
                    : beatmapContent.First().Value);
        }

        protected static string RenderContainer(string title, params string[] contents) =>
            Div("overview-container",
                Div("overview-container-title",
                    Encode(title)),
                Div("overview-fields",
                    contents));

        protected static string RenderField(string title, params string[] contents) =>
            Div("overview-field",
                Div("overview-field-title",
                    Encode(title)),
                Div("overview-field-content",
                    contents));

        protected static string RenderClosedField(string title, params string[] contents) =>
            Div("overview-field",
                Div("overview-field-title",
                    Encode(title)),
                DivAttr("overview-field-content", "style=\"display: none;\"",
                    contents));
    }
}