using System.Collections.Generic;
using MapsetVerifier.Snapshots.Objects;

namespace MapsetVerifier.Snapshots.Translators
{
    public class GeneralTranslator : DiffTranslator
    {
        public override string Section => "General";

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> diffs)
        {
            foreach (var diff in Snapshotter.TranslateSettings(Section, diffs, TranslateKey))
                yield return diff;
        }

        private static string TranslateKey(string key) =>
            key == "AudioFilename" ? "Audio filename" :
            key == "AudioLeadIn" ? "Audio lead-in" :
            key == "PreviewTime" ? "Preview time" :
            key == "SampleSet" ? "Default sample set" :
            key == "StackLeniency" ? "Stack leniency" :
            key == "LetterboxInBreaks" ? "Letterboxing in breaks" :
            key == "WidescreenStoryboard" ? "Widescreen storyboard" :
            key == "StoryFireInFront" ? "Storyboard in front of combo fire" :
            key == "SpecialStyle" ? "Special N+1 style" :
            key == "UseSkinSprites" ? "Use skin sprites in storyboard" :
            key == "EpilepsyWarning" ? "Epilepsy warning" : key;
    }
}