using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Framework.Objects.Resources;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects;
using MapsetVerifier.Parser.Statics;
using MathNet.Numerics;
using Serilog;

namespace MapsetVerifier.Checks.AllModes.General.Audio
{
    [Check]
    public class CheckHitSoundDelay : GeneralCheck
    {
        public override CheckMetadata GetMetadata() =>
            new()
            {
                Category = "Audio",
                Message = "Delayed hit sounds.",
                Author = "Naxess",

                Documentation = new Dictionary<string, string>
                {
                    {
                        "Purpose",
                        @"
                        Ensuring hit sounds which are used on active hit objects provide proper feedback for how early or late the player clicked.

                        ![](assets/checks/all-modes-general-audio-hit-sound-delay-1.png ""A hit sound which is delayed by ~10 ms, as shown in Audacity. Note that audacity shows its timeline in seconds, so 0.005 means 5 ms."")
                        "
                    },
                    {
                        "Reasoning",
                        @"
                        By having delayed hit sounds, the feedback the player receives would be misleading them into 
                        thinking they clicked later than they actually did, which contradicts the purpose of having hit 
                        sounds in the first place."
                    },
                    {
                        "Exceptions",
                        @"
                        Cymbals/bell-like sounds (often finish/whistle respectively) usually have a small wind-up before their peak. This is often acceptable to keep, as it would sound wrong without.
                        
                        ![](assets/checks/all-modes-general-audio-hit-sound-delay-2.png ""A bell hit sound whose peak is delayed by ~21 ms, which was considered fine due to the nature of the sound requiring a wind-up."")

                        The default `normal-hitfinish.wav` has a delay of ~6 ms, but is used by the game itself, so copying this and using as a custom sample is acceptable.
                        
                        ![](assets/checks/all-modes-general-audio-hit-sound-delay-3.png ""A spectrogram of the default `normal-hitfinish.wav`."")
                        "
                    },
                },
            };

        public override Dictionary<string, IssueTemplate> GetTemplates() =>
            new()
            {
                {
                    "Pure Delay",
                    new IssueTemplate(
                        Issue.Level.Problem,
                        "\"{0}\" has a {1} ms period of complete silence at the start.",
                        "path",
                        "pure delay"
                    ).WithCause(
                        "A hit sound file used on an active hit object has a definite delay (complete silence) of at least 5 ms."
                    )
                },
                {
                    "Delay",
                    new IssueTemplate(
                        Issue.Level.Warning,
                        "\"{0}\" has a delay of ~{2} ms, of which {1} ms is complete silence. (Active at e.g. {3} in {4}.)",
                        "path",
                        "pure delay",
                        "delay",
                        "timestamp",
                        "difficulty"
                    ).WithCause(
                        "A hit sound file used on an active hit object has very low volume for ~5 ms or more."
                    )
                },
                {
                    "Minor Delay",
                    new IssueTemplate(
                        Issue.Level.Minor,
                        "\"{0}\" has a delay of ~{2} ms, of which {1} ms is complete silence.",
                        "path",
                        "pure delay",
                        "delay"
                    ).WithCause("Same as the regular delay, except anything between 1 to 5 ms.")
                },
                {
                    "Unable to check",
                    new IssueTemplate(
                        Issue.Level.Error,
                        Common.FILE_EXCEPTION_MESSAGE,
                        "path"
                    ).WithCause("There was an error parsing a hit sound file.")
                },
            };

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            var activeHitObjectByFile = GetActiveHitObjectByFile(beatmapSet);

            foreach (var hsFile in beatmapSet.HitSoundFiles)
            {
                activeHitObjectByFile.TryGetValue(GetBaseName(hsFile), out var hitObjectActiveAt);

                if (hitObjectActiveAt == null)
                    // Hit sound is never active, so delay does not matter.
                    continue;

                var hsPath = Path.Combine(beatmapSet.SongPath, hsFile);

                List<float[]> peaks = [];
                Exception? exception = null;

                try
                {
                    peaks = AudioFileCache.GetPeaks(hsPath);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                if (exception == null)
                {
                    if (!(peaks.Count > 0) || !(peaks.Sum(peak => peak.Sum()) > 0))
                        // Muted files don't have anything to be delayed, hence ignore.
                        continue;

                    double maxStrength = peaks.Select(value => Math.Abs(value.Sum())).Max();

                    var delay = 0;
                    var pureDelay = 0;
                    double strength = 0;

                    while (delay + pureDelay < peaks.Count)
                    {
                        strength += Math.Abs(peaks[delay].Sum());

                        if (strength >= maxStrength / 2)
                            break;

                        strength *= 0.95;

                        // The delay added by MP3 encoding still has very slight volume where it's basically silent.
                        if (strength < 0.001)
                        {
                            strength = 0;
                            ++pureDelay;
                        }

                        ++delay;
                    }

                    if (pureDelay >= 5)
                        yield return new Issue(
                            GetTemplate("Pure Delay"),
                            null,
                            hsFile,
                            $"{pureDelay:0.##}"
                        );
                    else if (delay + pureDelay >= 5)
                        yield return new Issue(
                            GetTemplate("Delay"),
                            null,
                            hsFile,
                            $"{pureDelay:0.##}",
                            $"{delay:0.##}",
                            Timestamp.Get(hitObjectActiveAt),
                            hitObjectActiveAt.beatmap
                        );
                    else if (delay + pureDelay >= 1)
                        yield return new Issue(
                            GetTemplate("Minor Delay"),
                            null,
                            hsFile,
                            $"{pureDelay:0.##}",
                            $"{delay:0.##}"
                        );
                }
                else
                {
                    Log.Error(exception, "Couldn't check hitsound file");
                    yield return new Issue(GetTemplate("Unable to check"), null, hsFile);
                }
            }
        }

        /// <summary>
        ///     Builds a lookup from hit sound base file name (no extension, matching
        ///     <see cref="HitSample.GetFileName" />) to the first active hit object using it, in a
        ///     single pass over every beatmap's hit objects - avoids rescanning the whole hit object
        ///     list once per hit sound file, which for large mapsets/marathon maps dominates runtime.
        /// </summary>
        private static Dictionary<string, HitObject> GetActiveHitObjectByFile(BeatmapSet beatmapSet)
        {
            var result = new Dictionary<string, HitObject>();

            foreach (var beatmap in beatmapSet.Beatmaps)
            foreach (var hitObject in beatmap.HitObjects)
            {
                if (hitObject is Spinner)
                    continue;

                // Only the edge at which the object is clicked is considered active.
                foreach (var sample in hitObject.usedHitSamples)
                {
                    if (
                        sample.HitSource != HitSample.HitSourceType.Edge
                        || !sample.Time.AlmostEqual(hitObject.time)
                    )
                        continue;

                    var fileName = sample.GetFileName();

                    if (fileName != null)
                        result.TryAdd(fileName, hitObject);
                }
            }

            return result;
        }

        /// <summary>
        ///     Returns the part of a hit sound file name before its extension, matching what
        ///     <see cref="HitSample.SameFileName" /> compares against (<see cref="HitSample.GetFileName" />
        ///     never contains a dot, so splitting at the first dot is exact).
        /// </summary>
        private static string GetBaseName(string hitSoundFile)
        {
            var lower = hitSoundFile.ToLower();
            var dotIndex = lower.IndexOf('.');

            return dotIndex < 0 ? lower : lower[..dotIndex];
        }
    }
}
