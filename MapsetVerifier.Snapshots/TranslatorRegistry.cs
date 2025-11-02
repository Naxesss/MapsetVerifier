using System.Collections.Generic;
using MapsetVerifier.Snapshots.Objects;
using MapsetVerifier.Snapshots.Translators;

namespace MapsetVerifier.Snapshots
{
    public static class TranslatorRegistry
    {
        private static readonly List<DiffTranslator> translators = new();
        private static bool initialized;

        public static void InitalizeTranslators()
        {
            if (initialized)
                return;

            RegisterTranslator(new ColoursTranslator());
            RegisterTranslator(new DifficultyTranslator());
            RegisterTranslator(new EditorTranslator());
            RegisterTranslator(new EventsTranslator());
            RegisterTranslator(new FilesTranslator());
            RegisterTranslator(new GeneralTranslator());
            RegisterTranslator(new HitObjectsTranslator());
            RegisterTranslator(new MetadataTranslator());
            RegisterTranslator(new TimingTranslator());

            initialized = true;
        }

        public static void RegisterTranslator(DiffTranslator translator) => translators.Add(translator);

        public static IEnumerable<DiffTranslator> GetTranslators()
        {
            InitalizeTranslators();

            return new List<DiffTranslator>(translators);
        }
    }
}