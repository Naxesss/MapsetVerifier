namespace MapsetVerifier.Framework.Objects.Resources
{
    /// <summary>
    ///     Turns interleaved samples, given a chunk at a time, into the peak of each channel per ms,
    ///     without keeping the samples. A trailing partial ms is left out.
    /// </summary>
    internal sealed class AudioPeakAccumulator(int channels, int frequency)
    {
        private readonly double samplesPerMs = frequency / 1000.0;
        private readonly List<float[]> peaks = [];

        private float[] levels = new float[channels];
        private int channel;
        private long frame;
        private int ms;

        public void Add(ReadOnlySpan<float> samples)
        {
            if (frequency <= 0)
                return;

            foreach (var sample in samples)
            {
                if (channel == 0)
                    CompleteFinishedMs();

                var abs = Math.Abs(sample);

                if (abs > levels[channel])
                    levels[channel] = abs;

                if (++channel == channels)
                {
                    channel = 0;
                    ++frame;
                }
            }
        }

        public List<float[]> Finish()
        {
            if (frequency <= 0)
                return [];

            CompleteFinishedMs();

            // A ms can round down to end on the last frame while still reaching past the audio.
            var msCount = (int)(frame / samplesPerMs);

            if (peaks.Count > msCount)
                peaks.RemoveRange(msCount, peaks.Count - msCount);

            return peaks;
        }

        private void CompleteFinishedMs()
        {
            while (frame >= (int)((ms + 1) * samplesPerMs))
            {
                peaks.Add(levels);
                levels = new float[channels];
                ++ms;
            }
        }
    }
}
