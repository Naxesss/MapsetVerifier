using System.Collections.Generic;
using MapsetVerifier.Snapshots.Objects;

namespace MapsetVerifier.Snapshots.Translators
{
    public class ColoursTranslator : DiffTranslator
    {
        public override string Section => "Colours";

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> diffs)
        {
            foreach (var diff in Snapshotter.TranslateSettings(Section, diffs, TranslateKey))
                yield return diff;
        }

        private static string TranslateKey(string key) =>
            key == "Combo1" ? "Combo 1" :
            key == "Combo2" ? "Combo 2" :
            key == "Combo3" ? "Combo 3" :
            key == "Combo4" ? "Combo 4" :
            key == "Combo5" ? "Combo 5" :
            key == "Combo6" ? "Combo 6" :
            key == "Combo7" ? "Combo 7" :
            key == "Combo8" ? "Combo 8" :
            key == "Combo9" ? "Combo 9" :
            key == "SliderBody" ? "Slider body" :
            key == "SliderTrackOverride" ? "Slider track override" :
            key == "SliderBorder" ? "Slider border" : key;
    }
}