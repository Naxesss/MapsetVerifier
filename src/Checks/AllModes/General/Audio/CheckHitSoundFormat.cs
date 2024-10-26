using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ManagedBass;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;
using MathNet.Numerics;

namespace MapsetVerifier.Checks.AllModes.General.Audio
{
    [Check]
    public class CheckHitSoundFormat : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Audio",
                Message = "Incorrect hit sound format.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Discourages potentially detrimental file formats for hit sound files.
                    <image-right>
                        https://i.imgur.com/yAF6pEq.png
                        One of the hit sound files using the MP3 extension, which usually means it's also an MP3 format.
                    </image>"
                    },
                    {
                        "Reasoning",
                        @"
                    The MP3 format often includes inherent delays. As such, the Wave or OGG format is preferred for 
                    any hit sound file.
                    <note>
                        Passive objects such as slider tails are not clicked and as such do not need accurate 
                        feedback and may use the mp3 format because of this.
                    </note>
                    <note>
                        Note that extension is not the same thing as format. If you take an MP3 file and change its 
                        extension to "".wav"", for example, it will still be an MP3 file. To change the format of a 
                        file you need to re-encode it.
                    </note>"
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "mp3",
                    new IssueTemplate(Issue.Level.Problem, "\"{0}\" is using the MP3 format and is used for active hit sounding, see {1} in {2} for example.", "path", "timestamp - ", "beatmap").WithCause("A hit sound file is using the MP3 format.")
                },

                {
                    "Unexpected Format",
                    new IssueTemplate(Issue.Level.Warning, "\"{0}\" is using an unexpected format: \"{1}\".", "path", "actual format").WithCause("A hit sound file is using a format which is neither OGG, Wave, or MP3.")
                },

                {
                    "Incorrect Extension",
                    new IssueTemplate(Issue.Level.Warning, "\"{0}\" is using the {1} format, but doesn't use the .wav or .ogg extension.", "path", "actual format").WithCause("A hit sound file is using an incorrect extension.")
                },

                {
                    "Exception",
                    new IssueTemplate(Issue.Level.Error, Common.FILE_EXCEPTION_MESSAGE, "path", "exception info").WithCause("An error occurred trying to check the format of a hit sound file.")
                }
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            if (beatmapSet.HitSoundFiles == null)
                yield break;

            foreach (var hitSoundFile in beatmapSet.HitSoundFiles)
            {
                var fullPath = Path.Combine(beatmapSet.SongPath, hitSoundFile);

                ChannelType actualFormat = 0;
                Exception exception = null;

                try
                {
                    actualFormat = AudioBASS.GetFormat(fullPath);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                if (exception != null)
                {
                    yield return new Issue(GetTemplate("Exception"), null, hitSoundFile, Common.ExceptionTag(exception));

                    continue;
                }

                // The .mp3 format includes inherent delays and are as such not fit for active hit sounding.
                if (actualFormat == ChannelType.MP3)
                {
                    var hitObjectActiveAt = GetHitObjectActiveAt(beatmapSet, hitSoundFile);

                    if (hitObjectActiveAt != null)
                        yield return new Issue(GetTemplate("mp3"), null, hitSoundFile, Timestamp.Get(hitObjectActiveAt), hitObjectActiveAt.beatmap);
                }
                else
                {
                    if ((ChannelType.Wave & actualFormat) == 0 && (ChannelType.OGG & actualFormat) == 0)
                        yield return new Issue(GetTemplate("Unexpected Format"), null, hitSoundFile, AudioBASS.EnumToString(actualFormat));
                    else if (!hitSoundFile.ToLower().EndsWith(".wav") && !hitSoundFile.ToLower().EndsWith(".ogg"))
                        yield return new Issue(GetTemplate("Incorrect Extension"), null, hitSoundFile, AudioBASS.EnumToString(actualFormat));
                }
            }
        }

        private static HitObject GetHitObjectActiveAt(BeatmapSet beatmapSet, string hitSoundFile)
        {
            foreach (var beatmap in beatmapSet.Beatmaps)
                foreach (var hitObject in beatmap.HitObjects)
                {
                    if (hitObject is Spinner)
                        continue;

                    // Only the edge at which the object is clicked is considered active.
                    if (hitObject.usedHitSamples.Any(sample => sample.Time.AlmostEqual(hitObject.time) && sample.HitSource == HitSample.HitSourceType.Edge && sample.SameFileName(hitSoundFile)))
                        return hitObject;
                }

            return null;
        }
    }
}