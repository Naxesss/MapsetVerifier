using System.Globalization;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.Tests;

/// <summary> Fluent builder for minimal <c>.osu</c> files used by check tests. </summary>
public sealed class OsuBuilder
{
    public static string DefaultTimingPoint => TestTimingPoints.Uninherited(0, 500);
    public static string DefaultHitObject => TestHitObjects.Circle(1000);

    private string? rawContent;

    private Beatmap.Mode mode = Beatmap.Mode.Standard;
    private string audioFilename = "audio.mp3";
    private int? countdown;
    private string title = "MapsetVerifier";
    private string? titleUnicode;
    private string artist = "MapsetVerifier";
    private string creator = "Tests";
    private string version = "Test";
    private string? tags;
    private float circleSize = 4;
    private float hpDrainRate = 5;
    private float overallDifficulty = 5;
    private float approachRate = 5;
    private float sliderMultiplier = 1.4f;
    private float sliderTickRate = 1;
    private float? stackLeniency;
    private readonly List<string> events = [];
    private readonly List<string> colours = [];
    private readonly List<string> timingPoints = [];
    private readonly List<string> hitObjects = [];

    public static OsuBuilder Raw(string osuContent) => new() { rawContent = osuContent };

    public OsuBuilder Mode(Beatmap.Mode mode)
    {
        this.mode = mode;
        return this;
    }

    public OsuBuilder AudioFilename(string audioFilename)
    {
        this.audioFilename = audioFilename;
        return this;
    }

    public OsuBuilder Countdown(int countdown)
    {
        this.countdown = countdown;
        return this;
    }

    public OsuBuilder Title(string title)
    {
        this.title = title;
        return this;
    }

    public OsuBuilder TitleUnicode(string titleUnicode)
    {
        this.titleUnicode = titleUnicode;
        return this;
    }

    public OsuBuilder Artist(string artist)
    {
        this.artist = artist;
        return this;
    }

    public OsuBuilder Creator(string creator)
    {
        this.creator = creator;
        return this;
    }

    public OsuBuilder Version(string version)
    {
        this.version = version;
        return this;
    }

    public OsuBuilder Tags(string tags)
    {
        this.tags = tags;
        return this;
    }

    public OsuBuilder CircleSize(float circleSize)
    {
        this.circleSize = circleSize;
        return this;
    }

    public OsuBuilder HP(float hpDrainRate)
    {
        this.hpDrainRate = hpDrainRate;
        return this;
    }

    public OsuBuilder OD(float overallDifficulty)
    {
        this.overallDifficulty = overallDifficulty;
        return this;
    }

    public OsuBuilder AR(float approachRate)
    {
        this.approachRate = approachRate;
        return this;
    }

    public OsuBuilder SliderMultiplier(float sliderMultiplier)
    {
        this.sliderMultiplier = sliderMultiplier;
        return this;
    }

    public OsuBuilder SliderTickRate(float sliderTickRate)
    {
        this.sliderTickRate = sliderTickRate;
        return this;
    }

    public OsuBuilder StackLeniency(float stackLeniency)
    {
        this.stackLeniency = stackLeniency;
        return this;
    }

    public OsuBuilder Events(params string[] lines)
    {
        events.AddRange(lines);
        return this;
    }

    public OsuBuilder Events(IEnumerable<string> lines)
    {
        events.AddRange(lines);
        return this;
    }

    public OsuBuilder Colours(params string[] lines)
    {
        colours.AddRange(lines);
        return this;
    }

    public OsuBuilder Colours(IEnumerable<string> lines)
    {
        colours.AddRange(lines);
        return this;
    }

    public OsuBuilder TimingPoints(params string[] lines)
    {
        timingPoints.AddRange(lines);
        return this;
    }

    public OsuBuilder TimingPoints(IEnumerable<string> lines)
    {
        timingPoints.AddRange(lines);
        return this;
    }

    public OsuBuilder HitObjects(params string[] lines)
    {
        hitObjects.AddRange(lines);
        return this;
    }

    public OsuBuilder HitObjects(IEnumerable<string> lines)
    {
        hitObjects.AddRange(lines);
        return this;
    }

    public OsuBuilder WithDefaultTiming() => TimingPoints(DefaultTimingPoint);

    public OsuBuilder WithDefaultHitObject() => HitObjects(DefaultHitObject);

    public string Build()
    {
        if (rawContent != null)
            return rawContent;

        var inv = CultureInfo.InvariantCulture;
        var lines = new List<string>
        {
            "osu file format v14",
            "[General]",
            $"AudioFilename: {audioFilename}",
            $"Mode: {(int)mode}",
        };

        if (countdown != null)
            lines.Add($"Countdown: {countdown.Value}");

        lines.Add("[Metadata]");
        lines.Add($"Title:{title}");
        if (titleUnicode != null)
            lines.Add($"TitleUnicode:{titleUnicode}");
        lines.Add($"Artist:{artist}");
        lines.Add($"Creator:{creator}");
        lines.Add($"Version:{version}");
        if (tags != null)
            lines.Add($"Tags:{tags}");

        lines.Add("[Difficulty]");
        lines.Add($"CircleSize:{circleSize.ToString(inv)}");
        lines.Add($"HPDrainRate:{hpDrainRate.ToString(inv)}");
        lines.Add($"OverallDifficulty:{overallDifficulty.ToString(inv)}");
        lines.Add($"ApproachRate:{approachRate.ToString(inv)}");
        lines.Add($"SliderMultiplier:{sliderMultiplier.ToString(inv)}");
        lines.Add($"SliderTickRate:{sliderTickRate.ToString(inv)}");
        if (stackLeniency != null)
            lines.Add($"StackLeniency:{stackLeniency.Value.ToString(inv)}");

        lines.Add("[Events]");
        lines.AddRange(events);

        if (colours.Count > 0)
        {
            lines.Add("[Colours]");
            lines.AddRange(colours);
        }

        lines.Add("[TimingPoints]");
        lines.AddRange(timingPoints);
        lines.Add("[HitObjects]");
        lines.AddRange(hitObjects);

        return string.Join("\n", lines);
    }
}
