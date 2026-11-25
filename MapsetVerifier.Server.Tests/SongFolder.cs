using SkiaSharp;
using Xunit;

// The check registry, the folder scan cache and the snapshot store are process-wide state.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MapsetVerifier.Server.Tests;

/// <summary> A throwaway folder standing in for a mapset, or for a library of them. </summary>
public sealed class SongFolder : IDisposable
{
    private const int SampleRate = 44100;

    public SongFolder()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "MapsetVerifierServerTests",
            Guid.NewGuid().ToString("N")
        );
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public string Write(string name, string content = "")
    {
        var path = Prepare(name);
        File.WriteAllText(path, content);

        return path;
    }

    public string Write(string name, byte[] content)
    {
        var path = Prepare(name);
        File.WriteAllBytes(path, content);

        return path;
    }

    private string Prepare(string name)
    {
        var path = System.IO.Path.Combine([Path, .. name.Split('/')]);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);

        return path;
    }

    public static string Osu(
        string version = "Hard",
        int mode = 0,
        string[]? hitObjects = null,
        string[]? timingPoints = null,
        string[]? events = null,
        string[]? colours = null,
        string[]? general = null,
        string title = "Title",
        string artist = "Artist",
        string creator = "Creator",
        int beatmapId = 0,
        int beatmapSetId = -1,
        string circleSize = "4"
    ) =>
        string.Join(
            "\n",
            [
                "osu file format v14",
                "",
                "[General]",
                $"Mode: {mode}",
                .. general ?? [],
                "",
                "[Metadata]",
                $"Title:{title}",
                $"Artist:{artist}",
                $"Creator:{creator}",
                $"Version:{version}",
                $"BeatmapID:{beatmapId}",
                $"BeatmapSetID:{beatmapSetId}",
                "",
                "[Difficulty]",
                "HPDrainRate:5",
                $"CircleSize:{circleSize}",
                "OverallDifficulty:6",
                "ApproachRate:7",
                "SliderMultiplier:1.4",
                "SliderTickRate:1",
                "",
                "[Events]",
                .. events ?? [],
                "",
                "[TimingPoints]",
                .. timingPoints ?? ["0,500,4,2,0,100,1,0"],
                "",
                "[Colours]",
                .. colours ?? [],
                "",
                "[HitObjects]",
                .. hitObjects ?? ["256,192,1000,5,0,0:0:0:0:", "256,192,2000,1,0,0:0:0:0:"],
            ]
        );

    public static byte[] Image(
        int width,
        int height,
        SKEncodedImageFormat format = SKEncodedImageFormat.Png
    )
    {
        using var bitmap = new SKBitmap(width, height);
        using var data = bitmap.Encode(format, 85);

        return data.ToArray();
    }

    /// <summary> 16 bit PCM, with each channel's sample given by its time in seconds. </summary>
    public static byte[] Wave(double seconds, params Func<double, double>[] channels)
    {
        var frames = (int)(seconds * SampleRate);
        var dataSize = frames * channels.Length * 2;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels.Length);
        writer.Write(SampleRate);
        writer.Write(SampleRate * channels.Length * 2);
        writer.Write((short)(channels.Length * 2));
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(dataSize);

        for (var i = 0; i < frames; ++i)
            foreach (var channel in channels)
                writer.Write((short)Math.Round(channel((double)i / SampleRate) * short.MaxValue));

        return stream.ToArray();
    }

    public static Func<double, double> Sine(double amplitude, double frequency = 1000) =>
        time => amplitude * Math.Sin(2 * Math.PI * frequency * time);
}
