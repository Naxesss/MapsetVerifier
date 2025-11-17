using System;
using System.Collections.Generic;
using System.Linq;
using MapsetVerifier.Parser.Objects;

namespace MapsetVerifier.Framework.Objects.Metadata
{
    public class BeatmapCheckMetadata : CheckMetadata
    {
        /// <summary>
        ///     Can be initialized like this:
        ///     <para />
        ///     new GeneralCheckMetadata() { Category = "", Message = "", Author = "", ...  }
        /// </summary>
        public BeatmapCheckMetadata() { }

        /// <summary> The mode(s) this check applies to, by default all. </summary>
        public Beatmap.Mode[] Modes { get; set; } =
        [
            Beatmap.Mode.Standard,
            Beatmap.Mode.Taiko,
            Beatmap.Mode.Catch,
            Beatmap.Mode.Mania
        ];

        /// <summary> The difficulties this check applies to, by default all. </summary>
        public Beatmap.Difficulty[] Difficulties { get; set; } =
        [
            Beatmap.Difficulty.Easy,
            Beatmap.Difficulty.Normal,
            Beatmap.Difficulty.Hard,
            Beatmap.Difficulty.Insane,
            Beatmap.Difficulty.Expert,
            Beatmap.Difficulty.Ultra
        ];

        public override string GetMode()
        {
            if (Modes.Contains(Beatmap.Mode.Standard) && Modes.Contains(Beatmap.Mode.Taiko) && Modes.Contains(Beatmap.Mode.Catch) && Modes.Contains(Beatmap.Mode.Mania))
                return "All Modes";

            if (Modes.Length == 0)
                return "No Modes";

            var modes = Modes.Select(Enum.GetName)
                .OfType<string>()
                .ToList();

            return string.Join(" ", modes);
        }
    }
}