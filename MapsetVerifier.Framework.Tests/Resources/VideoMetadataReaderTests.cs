using MapsetVerifier.Framework.Objects.Resources;
using Xunit;

namespace MapsetVerifier.Framework.Tests.Resources
{
    public class VideoMetadataReaderTests : IDisposable
    {
        private readonly string workingDirectory = Directory
            .CreateTempSubdirectory("mv-video-tests")
            .FullName;

        public void Dispose()
        {
            try
            {
                Directory.Delete(workingDirectory, true);
            }
            catch (IOException)
            {
                // A leftover temporary directory is not worth failing a test run over.
            }
        }

        private string WriteFile(string fileName, byte[] content)
        {
            var path = Path.Combine(workingDirectory, fileName);
            File.WriteAllBytes(path, content);

            return path;
        }

        [Fact]
        public void Read_Mp4_ReturnsResolutionCodecAndFrameRate()
        {
            var path = WriteFile("video.mp4", new Mp4Fixture().Build());

            var metadata = VideoMetadataReader.Read(path);

            Assert.Equal("MP4", metadata.Container);
            Assert.Equal("mp4", metadata.Source);
            Assert.Equal(1280, metadata.Width);
            Assert.Equal(720, metadata.Height);
            Assert.Equal(5000, metadata.DurationMs);
            Assert.Equal(30, metadata.FrameRate);
            Assert.False(metadata.IsVariableFrameRate);
            Assert.Equal("H.264 / AVC", metadata.VideoCodec);
            Assert.Equal("High@L4.0", metadata.VideoCodecProfile);
            Assert.False(metadata.HasAudioTrack);
        }

        [Fact]
        public void Read_Mp4_DerivesBitratesFromSampleSizes()
        {
            var path = WriteFile("video.mp4", new Mp4Fixture().Build());

            var metadata = VideoMetadataReader.Read(path);

            // 150 samples of 1000 bytes each over five seconds.
            Assert.Equal(240000, metadata.VideoBitrateBps);
            Assert.Equal(
                (long)Math.Round(new FileInfo(path).Length * 8 / 5.0),
                metadata.OverallBitrateBps
            );
        }

        [Fact]
        public void Read_Mp4WithAudioTrack_ReportsTheAudioTrack()
        {
            var path = WriteFile("video.mp4", new Mp4Fixture { WithAudioTrack = true }.Build());

            var metadata = VideoMetadataReader.Read(path);

            Assert.True(metadata.HasAudioTrack);
            Assert.Equal("AAC", metadata.AudioCodec);
            Assert.Equal(2, metadata.AudioChannels);
            Assert.Equal(44100, metadata.AudioSampleRate);
        }

        [Fact]
        public void Read_Mp4WithMixedSampleDurations_ReportsVariableFrameRate()
        {
            var path = WriteFile(
                "video.mp4",
                new Mp4Fixture { TimeToSample = [(75, 1000), (75, 1200)] }.Build()
            );

            var metadata = VideoMetadataReader.Read(path);

            Assert.True(metadata.IsVariableFrameRate);
            Assert.NotNull(metadata.FrameRate);
        }

        [Fact]
        public void Read_Mp4WithTrailingOddSample_DoesNotReportVariableFrameRate()
        {
            var path = WriteFile(
                "video.mp4",
                new Mp4Fixture { TimeToSample = [(149, 1000), (1, 1500)] }.Build()
            );

            var metadata = VideoMetadataReader.Read(path);

            Assert.False(metadata.IsVariableFrameRate);
        }

        [Fact]
        public void Read_Mp4WithSixtyFourBitBoxSize_IsStillParsed()
        {
            var path = WriteFile("video.mp4", new Mp4Fixture { UseLargeMoovBox = true }.Build());

            var metadata = VideoMetadataReader.Read(path);

            Assert.Equal("mp4", metadata.Source);
            Assert.Equal(1280, metadata.Width);
            Assert.Equal(30, metadata.FrameRate);
        }

        [Fact]
        public void Read_FragmentedMp4_ReportsNoFrameRateAndWarns()
        {
            var path = WriteFile(
                "video.mp4",
                new Mp4Fixture { TimeToSample = [], WithMovieExtends = true }.Build()
            );

            var metadata = VideoMetadataReader.Read(path);

            Assert.Equal(1280, metadata.Width);
            Assert.Null(metadata.FrameRate);
            Assert.Contains(metadata.Warnings, warning => warning.Contains("fragmented MP4"));
        }

        [Fact]
        public void Read_Mp4WithoutCodecConfiguration_HasNoProfile()
        {
            var path = WriteFile("video.mp4", new Mp4Fixture { AvcConfiguration = null }.Build());

            var metadata = VideoMetadataReader.Read(path);

            Assert.Equal("H.264 / AVC", metadata.VideoCodec);
            Assert.Null(metadata.VideoCodecProfile);
        }

        [Fact]
        public void Read_Avi_ReturnsResolutionCodecAndFrameRate()
        {
            var path = WriteFile("video.avi", new AviFixture().Build());

            var metadata = VideoMetadataReader.Read(path);

            Assert.Equal("AVI", metadata.Container);
            Assert.Equal("avi", metadata.Source);
            Assert.Equal(1280, metadata.Width);
            Assert.Equal(720, metadata.Height);
            Assert.Equal(25, metadata.FrameRate);
            Assert.Equal(10000, metadata.DurationMs);
            Assert.Equal("MPEG-4 Visual", metadata.VideoCodec);
            Assert.False(metadata.HasAudioTrack);
        }

        [Fact]
        public void Read_AviWithAudioStream_ReportsTheAudioStream()
        {
            var path = WriteFile("video.avi", new AviFixture { WithAudioStream = true }.Build());

            var metadata = VideoMetadataReader.Read(path);

            Assert.True(metadata.HasAudioTrack);
            Assert.Equal("MP3", metadata.AudioCodec);
            Assert.Equal(2, metadata.AudioChannels);
            Assert.Equal(44100, metadata.AudioSampleRate);
        }

        [Fact]
        public void Read_Flv_ReturnsMetaDataProperties()
        {
            var path = WriteFile("video.flv", new FlvFixture().Build());

            var metadata = VideoMetadataReader.Read(path);

            Assert.Equal("FLV", metadata.Container);
            Assert.Equal("flv", metadata.Source);
            Assert.Equal(1280, metadata.Width);
            Assert.Equal(720, metadata.Height);
            Assert.Equal(10000, metadata.DurationMs);
            Assert.Equal(30, metadata.FrameRate);
            Assert.Equal(800000, metadata.VideoBitrateBps);
            Assert.Equal("H.264 / AVC", metadata.VideoCodec);
            Assert.True(metadata.HasAudioTrack);
            Assert.Equal("AAC", metadata.AudioCodec);
            Assert.Equal(2, metadata.AudioChannels);
        }

        [Fact]
        public void Read_FlvWithoutAudio_ReportsNoAudioTrack()
        {
            var path = WriteFile("video.flv", new FlvFixture { WithAudio = false }.Build());

            var metadata = VideoMetadataReader.Read(path);

            Assert.False(metadata.HasAudioTrack);
        }

        [Theory]
        [InlineData("empty.mp4", 0)]
        [InlineData("garbage.mp4", 64)]
        [InlineData("garbage.avi", 64)]
        [InlineData("garbage.flv", 64)]
        [InlineData("garbage.mkv", 64)]
        public void Read_UnreadableFile_FallsBackWithoutThrowing(string fileName, int size)
        {
            var content = new byte[size];

            for (var i = 0; i < size; ++i)
                content[i] = (byte)(i * 7);

            var path = WriteFile(fileName, content);

            var metadata = VideoMetadataReader.Read(path);

            Assert.NotNull(metadata);
            Assert.Equal(0, metadata.Width);
            Assert.NotEmpty(metadata.Warnings);
        }

        [Fact]
        public void Read_TruncatedMp4_FallsBackWithoutThrowing()
        {
            var full = new Mp4Fixture().Build();
            var path = WriteFile("truncated.mp4", full[..(full.Length / 3)]);

            var metadata = VideoMetadataReader.Read(path);

            Assert.NotNull(metadata);
            Assert.NotEmpty(metadata.Warnings);
        }

        [Fact]
        public void Read_Mp4NamedAsAvi_FollowsTheBytesRatherThanTheExtension()
        {
            // Beatmap sets carry MP4 files under an .avi name often enough to matter, and the
            // browser will only play them when they are recognized for what they are.
            var path = WriteFile("mislabeled.avi", new Mp4Fixture().Build());

            var metadata = VideoMetadataReader.Read(path);

            Assert.Equal("MP4", metadata.Container);
            Assert.Equal("mp4", metadata.Source);
            Assert.Equal(1280, metadata.Width);
        }

        [Theory]
        [InlineData("video.avi", "MP4")]
        [InlineData("video.txt", "MP4")]
        public void SniffContainer_ReturnsTheContainerOfTheBytes(string fileName, string expected)
        {
            var path = WriteFile(fileName, new Mp4Fixture().Build());

            Assert.Equal(expected, VideoMetadataReader.SniffContainer(path));
        }

        [Fact]
        public void SniffContainer_UnknownContainer_ReturnsNull()
        {
            var path = WriteFile("unknown.mkv", new byte[64]);

            Assert.Null(VideoMetadataReader.SniffContainer(path));
        }

        [Fact]
        public void SniffContainer_Avi_IsRecognized()
        {
            var path = WriteFile("video.avi", new AviFixture().Build());

            Assert.Equal("AVI", VideoMetadataReader.SniffContainer(path));
        }

        [Fact]
        public void Read_AlwaysReportsFileSize()
        {
            var path = WriteFile("video.mp4", new Mp4Fixture().Build());

            var metadata = VideoMetadataReader.Read(path);

            Assert.Equal(new FileInfo(path).Length, metadata.FileSizeBytes);
        }
    }
}
