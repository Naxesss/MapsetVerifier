using System.Collections.Generic;
using MapsetVerifier.Snapshots.Objects;

namespace MapsetVerifier.Snapshots.Translators
{
    public class DifficultyTranslator : DiffTranslator
    {
        public override string Section => "Difficulty";

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> diffs)
        {
            foreach (var diff in Snapshotter.TranslateSettings(Section, diffs, TranslateKey))
                yield return diff;
        }

        private static string TranslateKey(string key) =>
            key == "HPDrainRate" ? "HP drain rate" :
            key == "CircleSize" ? "Circle size" :
            key == "OverallDifficulty" ? "Overall difficulty" :
            key == "ApproachRate" ? "Approach rate" : key;
    }
}