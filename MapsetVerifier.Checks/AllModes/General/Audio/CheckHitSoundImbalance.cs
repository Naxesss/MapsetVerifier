using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Audio
{
    [Check]
    public class CheckHitSoundImbalance : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Audio",
                Message = "Imbalanced hit sounds.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Preventing the audio channels of hit sounds from being noticeably imbalanced, for example the left 
                    channel being twice as loud as the right.
                    <image>
                        https://i.imgur.com/6if5mJO.png
                        A hit sound which is much louder in the left channel compared to the right, as shown in Audacity.
                    </image>"
                    },
                    {
                        "Reasoning",
                        @"
                    Having noticeably imbalanced hit sounds is often jarring, especially if used frequently or consecutively."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Warning Silent",
                    new IssueTemplate(Issue.Level.Warning, "\"{0}\" is completely silent in the {1} channel.", "path", "left/right").WithCause("One of the channels of a hit sound has no volume, but still 2 channels.")
                },

                {
                    "Warning Common",
                    new IssueTemplate(Issue.Level.Warning, "\"{0}\" has a notably louder {1} channel. Used most commonly in {2}.", "path", "left/right", "[difficulty]").WithCause("One of the channels of a hit sound has at least half the total volume of the other. " + "The hit sound must also be used on average once every 10 seconds in a map.")
                },

                {
                    "Warning Timestamp",
                    new IssueTemplate(Issue.Level.Warning, "\"{0}\" has a notably louder {1} channel. Used most frequently leading up to {2}.", "path", "left/right", "timestamp in [difficulty]").WithCause("Same as the other check, except only happens when the hit sound is used frequently in a short timespan.")
                },

                {
                    "Minor",
                    new IssueTemplate(Issue.Level.Minor, "\"{0}\" has a notably louder {1} channel, not a huge deal in this case though.", "path", "left/right").WithCause("One of the channels of a hit sound has half the total volume of the other.")
                },

                {
                    "Unable to check",
                    new IssueTemplate(Issue.Level.Error, Common.FILE_EXCEPTION_MESSAGE, "path", "exception info").WithCause("There was an error parsing a hit sound file.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            foreach (var hsFile in beatmapSet.HitSoundFiles)
            {
                var hsPath = Path.Combine(beatmapSet.SongPath, hsFile);

                var channels = 0;
                List<float[]> peaks = [];
                Exception? exception = null;

                try
                {
                    channels = AudioBASS.GetChannels(hsPath);
                    peaks = AudioBASS.GetPeaks(hsPath);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                // Cannot yield in catch statements, hence externally handled.
                if (exception != null)
                {
                    yield return new Issue(GetTemplate("Unable to check"), null, hsFile, Common.ExceptionTag(exception));

                    continue;
                }

                // Mono cannot be imbalanced; same audio on both sides.
                if (channels < 2)
                    continue;

                // Silent audio cannot be imbalanced.
                if (peaks.Count == 0)
                    continue;

                var leftSum = peaks.Sum(peak => peak?[0] ?? 0);
                var rightSum = peaks.Sum(peak => peak.Length > 1 ? peak[1] : 0);

                if (leftSum == 0 || rightSum == 0)
                {
                    yield return new Issue(GetTemplate("Warning Silent"), null, hsFile, leftSum - rightSum > 0 ? "left" : "right");

                    continue;
                }

                // 2 would mean one is double the sum of the other.
                var relativeVolume = leftSum > rightSum ? leftSum / rightSum : rightSum / leftSum;

                if (relativeVolume < 2)
                    continue;

                // Imbalance is only an issue if it is used frequently in a short timespan or it's overall common.
                Common.CollectHitSoundFrequency(beatmapSet, hsFile, 14 / relativeVolume, out var mostFrequentTimestamp, out var uses);

                if (mostFrequentTimestamp != null)
                {
                    yield return new Issue(GetTemplate("Warning Timestamp"), null, hsFile, leftSum - rightSum > 0 ? "left" : "right", mostFrequentTimestamp);
                }
                else
                {
                    var mapCommonlyUsedIn = Common.GetBeatmapCommonlyUsedIn(beatmapSet, uses, 10000);

                    if (mapCommonlyUsedIn != null)
                        yield return new Issue(GetTemplate("Warning Common"), null, hsFile, leftSum - rightSum > 0 ? "left" : "right", mapCommonlyUsedIn);
                    else
                        yield return new Issue(GetTemplate("Minor"), null, hsFile, leftSum - rightSum > 0 ? "left" : "right");
                }
            }
        }
    }
}