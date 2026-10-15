using System.Globalization;

namespace MapsetVerifier.Framework.Objects.Resources
{
    /// <summary>
    /// Translates the raw codec identifiers found in media containers into names a mapper would
    /// recognize. Unknown identifiers are passed through as-is rather than hidden.
    /// </summary>
    internal static class MediaCodecNames
    {
        public static string? ForVideoFourCc(string? fourCc)
        {
            if (string.IsNullOrWhiteSpace(fourCc))
                return null;

            var normalized = fourCc.Trim().ToLowerInvariant();

            return normalized switch
            {
                "avc1" or "avc3" or "h264" or "x264" => "H.264 / AVC",
                "hev1" or "hvc1" or "h265" or "hevc" => "H.265 / HEVC",
                "vp08" or "vp8" => "VP8",
                "vp09" or "vp90" => "VP9",
                "av01" => "AV1",
                "mp4v" or "divx" or "dx50" or "xvid" or "fmp4" => "MPEG-4 Visual",
                "s263" or "h263" => "H.263",
                "mjpg" or "jpeg" => "Motion JPEG",
                "wmv1" or "wmv2" or "wmv3" or "wvc1" => "Windows Media Video",
                "mpg1" or "mpg2" or "mpeg" => "MPEG-1/2 Video",
                _ => fourCc.Trim(),
            };
        }

        public static string? ForAudioFourCc(string? fourCc)
        {
            if (string.IsNullOrWhiteSpace(fourCc))
                return null;

            var normalized = fourCc.Trim().ToLowerInvariant();

            return normalized switch
            {
                "mp4a" => "AAC",
                ".mp3" or "mp3" => "MP3",
                "ac-3" => "AC-3",
                "ec-3" => "E-AC-3",
                "alac" => "ALAC",
                "opus" => "Opus",
                "flac" => "FLAC",
                "samr" => "AMR",
                "sowt" or "twos" or "lpcm" or "in24" or "in32" => "PCM",
                _ => fourCc.Trim(),
            };
        }

        /// <summary> Maps a Windows <c>wFormatTag</c>, as used by AVI audio streams. </summary>
        public static string ForWaveFormatTag(ushort formatTag) =>
            formatTag switch
            {
                0x0001 => "PCM",
                0x0002 => "ADPCM",
                0x0003 => "IEEE Float",
                0x0055 => "MP3",
                0x0092 => "AC-3",
                0x00FF or 0x1600 or 0x1601 => "AAC",
                0x0160 or 0x0161 or 0x0162 or 0x0163 => "Windows Media Audio",
                0x2000 => "AC-3",
                0x2001 => "DTS",
                0x674F or 0x6750 or 0x6751 => "Vorbis",
                _ => $"Unknown (0x{formatTag:X4})",
            };

        /// <summary> Maps an FLV <c>videocodecid</c>. </summary>
        public static string ForFlvVideoCodecId(int codecId) =>
            codecId switch
            {
                1 => "JPEG",
                2 => "Sorenson H.263",
                3 => "Screen Video",
                4 => "VP6",
                5 => "VP6 with alpha",
                6 => "Screen Video 2",
                7 => "H.264 / AVC",
                12 => "H.265 / HEVC",
                _ => $"Unknown ({codecId})",
            };

        /// <summary> Maps an FLV <c>audiocodecid</c>. </summary>
        public static string ForFlvAudioCodecId(int codecId) =>
            codecId switch
            {
                0 => "PCM",
                1 => "ADPCM",
                2 => "MP3",
                3 => "PCM (little endian)",
                4 or 5 or 6 => "Nellymoser",
                7 => "G.711 A-law",
                8 => "G.711 mu-law",
                10 => "AAC",
                11 => "Speex",
                14 => "MP3 (8 kHz)",
                _ => $"Unknown ({codecId})",
            };

        public static string FormatAvcProfile(byte profileIndication, byte levelIndication)
        {
            var profile = profileIndication switch
            {
                66 => "Baseline",
                77 => "Main",
                88 => "Extended",
                100 => "High",
                110 => "High 10",
                122 => "High 4:2:2",
                244 => "High 4:4:4",
                _ => $"Profile {profileIndication}",
            };

            return $"{profile}@L{FormatLevel(levelIndication)}";
        }

        public static string FormatHevcProfile(byte profileSpaceTierIdc, byte generalLevelIdc)
        {
            var profileIdc = profileSpaceTierIdc & 0x1F;
            var isHighTier = (profileSpaceTierIdc & 0x20) != 0;

            var profile = profileIdc switch
            {
                1 => "Main",
                2 => "Main 10",
                3 => "Main Still Picture",
                _ => $"Profile {profileIdc}",
            };

            // HEVC levels are expressed in units of thirty, unlike AVC's tens.
            var level = (generalLevelIdc / 30.0).ToString("0.#", CultureInfo.InvariantCulture);

            return $"{profile}@L{level}{(isHighTier ? " High tier" : string.Empty)}";
        }

        private static string FormatLevel(byte levelIndication) =>
            (levelIndication / 10.0).ToString("0.0", CultureInfo.InvariantCulture);
    }
}
