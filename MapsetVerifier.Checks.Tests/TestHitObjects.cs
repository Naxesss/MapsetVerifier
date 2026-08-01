using System.Globalization;

namespace MapsetVerifier.Checks.Tests;

/// <summary>
/// Builds wiki-shaped <c>[HitObjects]</c> lines for check tests.
/// Field order: x,y,time,type,hitSound,objectParams,hitSample
/// </summary>
public static class TestHitObjects
{
    public static string Circle(
        int time,
        int x = 256,
        int y = 192,
        int hitSound = 0,
        string hitSample = "0:0:0:0:"
    ) => $"{x},{y},{time},1,{hitSound},{hitSample}";

    public static string Slider(
        int time,
        string curveType = "L",
        string curvePoints = "256:300",
        int slides = 1,
        double length = 100,
        int x = 256,
        int y = 192,
        int hitSound = 0,
        string? edgeSounds = null,
        string? edgeSets = null,
        string hitSample = "0:0:0:0:",
        int type = 2
    )
    {
        var edgeCount = slides + 1;
        edgeSounds ??= string.Join("|", Enumerable.Repeat("0", edgeCount));
        edgeSets ??= string.Join("|", Enumerable.Repeat("0:0", edgeCount));

        var inv = CultureInfo.InvariantCulture;
        return string.Join(
            ",",
            x.ToString(inv),
            y.ToString(inv),
            time.ToString(inv),
            type.ToString(inv),
            hitSound.ToString(inv),
            $"{curveType}|{curvePoints}",
            slides.ToString(inv),
            length.ToString(inv),
            edgeSounds,
            edgeSets,
            hitSample
        );
    }

    public static string Spinner(
        int time,
        int endTime,
        int x = 256,
        int y = 192,
        int hitSound = 0,
        string hitSample = "0:0:0:0:",
        int type = 8
    ) => $"{x},{y},{time},{type},{hitSound},{endTime},{hitSample}";
}
