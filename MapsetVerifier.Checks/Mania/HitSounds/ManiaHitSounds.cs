using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Checks.Mania.HitSounds
{
    /// <summary> Which part of osu!'s sample resolution a <see cref="ManiaSample" /> came from. </summary>
    public enum ManiaSampleKind
    {
        /// <summary> The hit normal, which every note plays regardless of its additions. </summary>
        HitNormal,

        /// <summary> A whistle, finish or clap addition. </summary>
        Addition,

        /// <summary> A file referenced by the note itself, replacing every other sample on that note. </summary>
        CustomFile,
    }

    /// <summary> A single sample a mania note plays. </summary>
    /// <param name="Name">
    ///     Lower case identity of the sample, used to tell samples apart. The file name without extension for
    ///     derived samples (e.g. <c>soft-hitclap2</c>), the referenced path for custom files.
    /// </param>
    /// <param name="Display"> How the sample is referred to in issue messages. </param>
    public record ManiaSample(
        string Name,
        string Display,
        ManiaSampleKind Kind,
        int CustomIndex,
        HitSample.SamplesetType Sampleset
    );

    /// <summary>
    ///     Resolves which sample files a mania note actually plays, shared by the mania hit sound checks so
    ///     they all agree on what a note sounds like.
    /// </summary>
    public static class ManiaHitSounds
    {
        private static readonly HitObject.HitSounds[] Additions =
        [
            HitObject.HitSounds.Whistle,
            HitObject.HitSounds.Finish,
            HitObject.HitSounds.Clap,
        ];

        /// <summary>
        ///     Returns every sample the given mania note plays. A note referencing a file plays only that file,
        ///     otherwise it plays the hit normal plus one sample per addition.
        /// </summary>
        public static IEnumerable<ManiaSample> GetSamples(HitObject hitObject)
        {
            if (hitObject.filename != null)
            {
                yield return new ManiaSample(
                    PathStatic.ParsePath(hitObject.filename)!,
                    hitObject.filename,
                    ManiaSampleKind.CustomFile,
                    0,
                    HitSample.SamplesetType.Auto
                );

                yield break;
            }

            // The object's own custom index takes priority over the line's, which GetCustomIndex handles, but
            // only when handed the same leniently looked up line that the sampleset resolution below uses.
            var line = hitObject.beatmap.GetTimingLine(hitObject.time, true);
            var customIndex = hitObject.GetCustomIndex(line);

            // The hit normal plays whether any addition is set, and never uses the addition sampleset.
            yield return Derive(
                hitObject.GetSampleset(),
                HitObject.HitSounds.Normal,
                customIndex,
                ManiaSampleKind.HitNormal
            );

            foreach (var addition in Additions)
            {
                if (!hitObject.HasHitSound(addition))
                    continue;

                yield return Derive(
                    hitObject.GetSampleset(true),
                    addition,
                    customIndex,
                    ManiaSampleKind.Addition
                );
            }
        }

        /// <summary>
        ///     Whether the mapset is expected to contain a file for this sample. Custom indexes of 0 and 1 fall
        ///     back to the player's skin or the game's own samples, so those are supposed to be absent.
        /// </summary>
        public static bool RequiresFileInMapset(this ManiaSample sample) =>
            sample.Kind == ManiaSampleKind.CustomFile
            || (sample.CustomIndex >= 2 && sample.Sampleset != HitSample.SamplesetType.Auto);

        /// <summary>
        ///     The name a file in the song folder needs for it to be this sample. Derived samples match on the
        ///     base name only, since osu! accepts any supported audio extension for them.
        /// </summary>
        public static string ExpectedFileName(this ManiaSample sample) =>
            sample.Kind == ManiaSampleKind.CustomFile
                ? PathStatic.ParsePath(PathStatic.CutPath(sample.Name))!
                : sample.Name;

        /// <summary>
        ///     The names to look a sample up by, matching <see cref="ExpectedFileName" />, for every file in the
        ///     song folder. Subfolders are included since osu! finds samples there too.
        /// </summary>
        public static HashSet<string> GetSampleFileNames(BeatmapSet beatmapSet)
        {
            var names = new HashSet<string>();

            foreach (var filePath in beatmapSet.SongFilePaths)
            {
                var fileName = PathStatic.CutPath(filePath);

                names.Add(PathStatic.ParsePath(fileName)!);
                names.Add(PathStatic.ParsePath(fileName, true)!);
            }

            return names;
        }

        /// <summary>
        ///     Groups hit objects by the time they're hit at. Mania notes stacked into a chord share an exact
        ///     time, and difficulties hit sounded by copying share it across the whole set.
        /// </summary>
        public static long TimeKey(double time) => (long)Math.Round(time);

        private static ManiaSample Derive(
            HitSample.SamplesetType sampleset,
            HitObject.HitSounds hitSound,
            int customIndex,
            ManiaSampleKind kind
        )
        {
            // Built through HitSample so the name matches how the parser names the same sample everywhere else.
            var name = new HitSample(
                customIndex,
                sampleset,
                hitSound,
                HitSample.HitSourceType.Edge,
                0
            ).GetFileName()!;

            return new ManiaSample(name, $"{name}.wav/ogg", kind, customIndex, sampleset);
        }
    }
}
