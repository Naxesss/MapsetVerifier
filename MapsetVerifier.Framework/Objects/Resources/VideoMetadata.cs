namespace MapsetVerifier.Framework.Objects.Resources
{
    /// <summary>
    /// Container-level metadata of a video file. Read straight from the container headers, so no
    /// frames are ever decoded and no native media library is needed.
    /// </summary>
    public sealed class VideoMetadata
    {
        /// <summary> Human-readable container name, e.g. "MP4". Falls back to the file extension. </summary>
        public string Container { get; internal set; } = "Unknown";

        /// <summary> Human-readable video codec, e.g. "H.264 / AVC", or the raw fourcc when unrecognized. </summary>
        public string? VideoCodec { get; internal set; }

        /// <summary> Profile and level of the video codec, e.g. "High@L4.0". Null when unavailable. </summary>
        public string? VideoCodecProfile { get; internal set; }

        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public double DurationMs { get; internal set; }

        /// <summary> Average frame rate, or null when the container does not let us derive it. </summary>
        public double? FrameRate { get; internal set; }

        public bool IsVariableFrameRate { get; internal set; }

        /// <summary> Bitrate of the video track alone, or null when the container does not expose it. </summary>
        public long? VideoBitrateBps { get; internal set; }

        /// <summary> Bitrate of the whole file (file size over duration). Always available. </summary>
        public long OverallBitrateBps { get; internal set; }

        public bool HasAudioTrack { get; internal set; }
        public string? AudioCodec { get; internal set; }
        public int AudioChannels { get; internal set; }
        public int AudioSampleRate { get; internal set; }
        public long FileSizeBytes { get; internal set; }

        /// <summary> Which parser produced this, e.g. "mp4", "avi", "flv" or "taglib". </summary>
        public string Source { get; internal set; } = "unknown";

        /// <summary> Notes about fields that could not be determined, shown to the user as-is. </summary>
        public List<string> Warnings { get; } = [];
    }
}
