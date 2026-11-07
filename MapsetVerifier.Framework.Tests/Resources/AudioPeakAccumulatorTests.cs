using MapsetVerifier.Framework.Objects.Resources;
using Xunit;

namespace MapsetVerifier.Framework.Tests.Resources
{
    public class AudioPeakAccumulatorTests
    {
        public static TheoryData<int, int, int, int> Inputs()
        {
            var inputs = new TheoryData<int, int, int, int>();

            foreach (var frequency in new[] { 8000, 22050, 44100, 48000, 11025, 500 })
            foreach (var channels in new[] { 1, 2, 6 })
            foreach (var frames in new[] { 0, 1, 43, 44, 45, 88, 89, 44100, 44101, 123457 })
            foreach (var chunkSize in new[] { 1, 7, 4096, 65536 })
                inputs.Add(frequency, channels, frames, chunkSize);

            return inputs;
        }

        [Theory]
        [MemberData(nameof(Inputs))]
        public void Peaks_MatchComputingThemFromAllSamples(
            int frequency,
            int channels,
            int frames,
            int chunkSize
        )
        {
            var samples = CreateSamples(frames * channels);

            var peaks = Accumulate(samples, channels, frequency, chunkSize);

            AssertSamePeaks(ComputeFromAllSamples(samples, channels, frequency), peaks);
        }

        [Fact]
        public void TrailingPartialFrame_IsLeftOut()
        {
            // Two complete stereo frames per ms, then half a frame much louder than the rest.
            var peaks = Accumulate([0.1f, 0.2f, 0.3f, 0.4f, 1f], 2, 2000, 65536);

            var peak = Assert.Single(peaks);
            Assert.Equal([0.3f, 0.4f], peak);
        }

        [Fact]
        public void InvalidFrequency_HasNoPeaks() =>
            Assert.Empty(Accumulate([0.5f, 0.5f], 1, 0, 65536));

        private static List<float[]> Accumulate(
            float[] samples,
            int channels,
            int frequency,
            int chunkSize
        )
        {
            var accumulator = new AudioPeakAccumulator(channels, frequency);

            for (var start = 0; start < samples.Length; start += chunkSize)
                accumulator.Add(samples.AsSpan(start, Math.Min(chunkSize, samples.Length - start)));

            return accumulator.Finish();
        }

        private static float[] CreateSamples(int count)
        {
            var random = new Random(count);
            var samples = new float[count];

            for (var i = 0; i < count; ++i)
                samples[i] = (float)(random.NextDouble() * 2 - 1);

            return samples;
        }

        private static void AssertSamePeaks(List<float[]> expected, List<float[]> actual)
        {
            Assert.Equal(expected.Count, actual.Count);

            for (var i = 0; i < expected.Count; ++i)
                Assert.Equal(expected[i], actual[i]);
        }

        /// <summary> The calculation with every sample in memory, as a reference for the expected result. </summary>
        private static List<float[]> ComputeFromAllSamples(
            float[] samples,
            int channels,
            int frequency
        )
        {
            if (samples.Length == 0)
                return [];

            var frameCount = samples.Length / channels;
            var samplesPerMs = frequency / 1000.0;
            var msCount = (int)(frameCount / samplesPerMs);
            var peaks = new List<float[]>(msCount);
            var frame = 0;

            for (var ms = 0; ms < msCount; ++ms)
            {
                var frameEnd = Math.Min((int)((ms + 1) * samplesPerMs), frameCount);
                var levels = new float[channels];

                for (; frame < frameEnd; ++frame)
                for (var channel = 0; channel < channels; ++channel)
                {
                    var abs = Math.Abs(samples[frame * channels + channel]);

                    if (abs > levels[channel])
                        levels[channel] = abs;
                }

                peaks.Add(levels);
            }

            return peaks;
        }
    }
}
