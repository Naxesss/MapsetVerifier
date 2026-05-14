using System.Text.RegularExpressions;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.TimingLines;

namespace MapsetVerifier.Checks.Utils;

public static class ManiaUtils
{
    /// <summary> https://stackoverflow.com/a/30300521 </summary>
    public static String WildCardToRegular(String value)
    {
        return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
    }

    /// <summary> Checks whether the Hitsound list of a Beatmapset has any acceptable hitnormal sample </summary>
    public static bool hasHitNormal(string hitsound)
    {
        if (Regex.IsMatch(hitsound.ToLower(), WildCardToRegular("*-hitnormal*")))
            return true;
        else
            return false;
    }

    /// <summary> Checks whether a specific sample is found in the hitNormal list </summary>
    public static bool isHitNormalInList(string sample, List<string> hitNormalList)
    {
        bool isInList = false;
        foreach (var hitNormal in hitNormalList)
            if (Regex.IsMatch(hitNormal, WildCardToRegular("*" + sample + "*")))
                isInList = true;

        return isInList;
    }

    /// <summary> Returns a list of all hitnormal files used </summary>
    public static IEnumerable<string> getHitNormalSamples(List<string> hitSoundList)
    {
        foreach (var hitSound in hitSoundList)
            if (hasHitNormal(hitSound))
                yield return hitSound.ToLower();
    }

    /// <summary> "osu!" calc to get the base BPM of a beatmap. All credits go to the original devs. For consistency sake, we'll be using their algorithm. Original method github.com/Naxesss/MapsetParser/blob/master/objects/TimingLine.cs </summary>
    public static double GetMostCommonBeatLength(Beatmap beatmap)
    {
        // The last playable time in the beatmap - the last timing point extends to this time.
        // Note: This is more accurate and may present different results because osu-stable didn't have the ability to calculate slider durations in this context.
        double lastTime =
            beatmap.HitObjects.LastOrDefault()?.GetEndTime()
            ?? beatmap.TimingLines.LastOrDefault()?.Offset
            ?? 0;
        double firstTime = 0;

        // TimingLine -> UninheritedLine cast conversion to fetch "beatLength" values
        List<UninheritedLine> uninheritedLines = beatmap
            .TimingLines.OfType<UninheritedLine>()
            .Cast<UninheritedLine>()
            .ToList();

        List<double> timeList = [];
        List<double> BPMList = [];

        bool first = true;
        double currentBPM = 0;
        foreach (var item in uninheritedLines)
        {
            if (!first && item.Offset > firstTime && item.Offset < lastTime)
            {
                if (BPMList.Any(item => item - currentBPM == 0))
                {
                    timeList[BPMList.IndexOf(currentBPM)] += item.Offset - firstTime;
                }
                else
                {
                    timeList.Add(item.Offset - firstTime);
                    BPMList.Add(currentBPM);
                }
                firstTime = item.Offset;
                currentBPM = Math.Round(item.bpm, 2);
            }
            else
            {
                first = false;
                firstTime = item.Offset;
                currentBPM = Math.Round(item.bpm, 2);
            }
        }
        if (BPMList.Any(item => Math.Abs(item - currentBPM) <= 1e-8))
        {
            timeList[BPMList.IndexOf(currentBPM)] += lastTime - firstTime;
        }
        else
        {
            timeList.Add(lastTime - firstTime);
            BPMList.Add(currentBPM);
        }

        return Math.Round(BPMList[timeList.IndexOf(timeList.Max())], 2);
    }

    /// <summary> Checks if two values are almost equal given a third delta value. </summary>
    public static bool almostEquals(double value1, double value2, double epsilon)
    {
        return Math.Abs(value1 - value2) <= epsilon;
    }

    public static int getColumn(HitObject hitObject, float keys)
    {
        // This is the * Magic Fix *
        return (int)hitObject.Position.X / (512 / (int)keys);
    }
}
