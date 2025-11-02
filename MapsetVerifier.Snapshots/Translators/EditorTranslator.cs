using System.Collections.Generic;
using MapsetVerifier.Snapshots.Objects;

namespace MapsetVerifier.Snapshots.Translators
{
    public class EditorTranslator : DiffTranslator
    {
        public override string Section => "Editor";

        public override IEnumerable<DiffInstance> Translate(IEnumerable<DiffInstance> diffs)
        {
            foreach (var diff in Snapshotter.TranslateSettings(Section, diffs, TranslateKey))
                yield return diff;
        }

        private static string TranslateKey(string key) =>
            key == "DistanceSpacing" ? "Distance spacing" :
            key == "BeatDivisor" ? "Beat snap divisor" :
            key == "GridSize" ? "Grid size" :
            key == "TimelineZoom" ? "Timeline zoom" : key;
    }
}