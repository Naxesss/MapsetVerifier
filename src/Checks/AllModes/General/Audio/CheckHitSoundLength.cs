using System;
using System.Collections.Generic;
using System.IO;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Checks.AllModes.General.Audio
{
    [Check]
    public class CheckHitSoundLength : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Audio",
                Message = "Too short hit sounds.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                    Ensuring hit sounds play consistently across multiple sound cards."
                    },
                    {
                        "Reasoning",
                        @"
                    Hit sounds shorter than ~25 ms (depending on soundcard) can result in no sound being played in-game. 
                    This makes it equivalent to a completely silent hit sound for some, while not for others. Muted hit 
                    sounds are fine having 0 ms duration though, since they don't play audio anyway.
                    <image>
                        https://i.imgur.com/y9Zmxp3.png
                        The 2.95 ms long hit sound from the example, as shown in Audacity.
                    </image>"
                    },
                    {
                        "Example",
                        @"""soft-hitnormal.wav"" is shorter than 25 ms (2.95 ms) in https://osu.ppy.sh/beatmapsets/1527, 
                    see 00:25:880 (1) - . Set music to 0% and effect to 100% for clarity."
                    }
                }
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Length",
                    new IssueTemplate(Issue.Level.Problem, "\"{0}\" is shorter than 25 ms ({1} ms).", "path", "length").WithCause("A hit sound file is shorter than 25 ms and longer than 0 ms.")
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

                double duration = 0;
                Exception exception = null;

                try
                {
                    duration = AudioBASS.GetDuration(hsPath);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                if (exception == null)
                {
                    // Greater than 0 since 44-byte muted hit sounds are fine.
                    if (duration < 25 && duration > 0)
                        yield return new Issue(GetTemplate("Length"), null, hsFile, $"{duration:0.##}");
                }
                else
                {
                    yield return new Issue(GetTemplate("Unable to check"), null, hsFile, Common.ExceptionTag(exception));
                }
            }
        }
    }
}