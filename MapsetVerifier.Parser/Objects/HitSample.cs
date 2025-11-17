using System.Text.RegularExpressions;
using static MapsetVerifier.Parser.Objects.HitObject;

namespace MapsetVerifier.Parser.Objects
{
    public class HitSample
    {
        /// <summary> Which type of hit sounds are used, does not affect hitnormal if addition. </summary>
        public enum SamplesetType
        {
            Auto,
            Normal,
            Soft,
            Drum
        }
        
        public enum HitSourceType
        {
            Edge,
            Body,
            Tick,
            Unknown
        }

        public int CustomIndex { get; }
        public HitSounds? HitSound { get; }
        public HitSourceType HitSource { get; }
        public SamplesetType? Sampleset { get; }
        public bool Taiko { get; }

        public double Time { get; }

        public HitSample(int customIndex, SamplesetType? sampleset, HitSounds? hitSound, HitSourceType hitSource, double time, bool taiko = false)
        {
            CustomIndex = customIndex;
            Sampleset = sampleset;
            HitSound = hitSound;
            HitSource = hitSource;
            Taiko = taiko;

            Time = time;
        }

        public HitSample(string fileName)
        {
            var regex = new Regex(@"(?i)^(taiko-)?(soft|normal|drum)-(hit(whistle|normal|finish)|slider(slide|whistle|tick))(\d+)?");

            var match = regex.Match(fileName);
            var groups = match.Groups;

            Taiko = groups[1].Success;
            Sampleset = ParseSampleset(groups[2].Value);
            HitSource = ParseHitSource(groups[3].Value);

            // Can either be part of "hit/.../" or "slider/.../"
            if (groups[4].Success)
                HitSound = ParseHitSound(groups[4].Value);
            else if (groups[5].Success)
                HitSound = ParseHitSound(groups[5].Value);
            else
                HitSound = null;

            CustomIndex = ParseCustomIndex(groups[6].Value);
        }

        /// <summary>
        ///     Returns the sampleset corresponding to the given text representation, e.g. "drum" or "soft".
        ///     Unrecognized representation returns null.
        /// </summary>
        private SamplesetType? ParseSampleset(string text)
        {
            var lowerText = text.ToLower();

            return lowerText == "soft" ? SamplesetType.Soft :
                lowerText == "normal" ? SamplesetType.Normal :
                lowerText == "drum" ? SamplesetType.Drum : (SamplesetType?)null;
        }

        /// <summary>
        ///     Returns the hit source corresponding to the given text representation, e.g. "hitnormal" or "sliderslide".
        ///     Unrecognized representation returns a hit source of type unknown.
        /// </summary>
        private HitSourceType ParseHitSource(string text)
        {
            var lowerText = text.ToLower();

            return lowerText.StartsWith("hit") ? HitSourceType.Edge :
                lowerText.StartsWith("slidertick") ? HitSourceType.Tick :
                lowerText.StartsWith("slider") ? HitSourceType.Body : HitSourceType.Unknown;
        }

        /// <summary>
        ///     Returns the hit sound corresponding to the given text representation, e.g. "whistle", "clap" or "finish".
        ///     Unrecognized representation, or N/A (e.g. sliderslide/tick), returns null.
        /// </summary>
        private HitSounds? ParseHitSound(string text)
        {
            var lowerText = text.ToLower();

            return lowerText == "normal" ? HitSounds.Normal :
                lowerText == "clap" ? HitSounds.Clap :
                lowerText == "whistle" ? HitSounds.Whistle :
                lowerText == "finish" ? HitSounds.Finish : (HitSounds?)null;
        }

        /// <summary> Returns the given text as an integer if possible, else 1 (i.e. implicit custom index). </summary>
        private static int ParseCustomIndex(string text)
        {
            try
            {
                return int.Parse(text);
            }
            catch
            {
                return 1;
            }
        }

        /// <summary> Returns the file name of this sample without extension, or null if no file is associated. </summary>
        public string? GetFileName()
        {
            var taikoString = Taiko ? "taiko-" : "";
            var samplesetString = Sampleset?.ToString().ToLower();
            string? hitSoundString = null;

            if (HitSound is { } hs)
            {
                hitSoundString = HitSource switch
                {
                    HitSourceType.Edge when hs != HitSounds.None => "hit" + hs.ToString().ToLower(),
                    HitSourceType.Body => "slider" + (hs == HitSounds.Whistle ? "whistle" : "slide"),
                    _ => null
                };
            }

            if (HitSource == HitSourceType.Tick)
                hitSoundString = "slidertick";

            var customIndexString = CustomIndex == 1 ? "" : CustomIndex.ToString();

            if (hitSoundString != null && samplesetString != null)
                return $"{taikoString}{samplesetString}-{hitSoundString}{customIndexString}";

            return null;
        }

        /// <summary>
        ///     Returns whether the sample file name is the same as the given file name (i.e. same sample file).
        ///     Ignores case sensitivity.
        /// </summary>
        public bool SameFileName(string fileNameWithExtension) => fileNameWithExtension.ToLower().StartsWith(GetFileName() + ".");
    }
}