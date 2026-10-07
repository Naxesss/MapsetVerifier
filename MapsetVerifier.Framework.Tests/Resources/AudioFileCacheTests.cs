using MapsetVerifier.Framework.Objects.Resources;
using Xunit;

namespace MapsetVerifier.Framework.Tests.Resources
{
    public class AudioFileCacheTests : IDisposable
    {
        private readonly string directory = Directory
            .CreateDirectory(
                Path.Combine(Path.GetTempPath(), "mv-audio-cache-" + Guid.NewGuid().ToString("N"))
            )
            .FullName;

        public void Dispose()
        {
            AudioFileCache.Clear();
            Directory.Delete(directory, recursive: true);
        }

        private string CreateFile(string name, byte[] content)
        {
            var path = Path.Combine(directory, name);
            File.WriteAllBytes(path, content);

            return path;
        }

        // Not decodable audio, mimicking a corrupt or mislabeled hit sound (BASS error "FileFormat").
        private string CreateBogusOgg(string name) =>
            CreateFile(name, "this is not an ogg file"u8.ToArray());

        [Fact]
        public void WarmUp_UnreadableFiles_DoesNotThrow()
        {
            var paths = new[]
            {
                CreateBogusOgg("soft-sliderslide.ogg"),
                CreateBogusOgg("soft-sliderslide3.ogg"),
            };

            var exception = Record.Exception(() => AudioFileCache.WarmUp(paths));

            Assert.Null(exception);
        }

        [Fact]
        public void WarmUp_EmptyFile_DoesNotThrow()
        {
            var path = CreateFile("empty.ogg", []);

            var exception = Record.Exception(() => AudioFileCache.WarmUp([path]));

            Assert.Null(exception);
        }

        [Fact]
        public void WarmUp_UnreadableFiles_StillReportsEveryFileAsWarmed()
        {
            var paths = new[]
            {
                CreateBogusOgg("a.ogg"),
                CreateBogusOgg("b.ogg"),
                CreateFile("empty.ogg", []),
            };
            var warmed = 0;

            AudioFileCache.WarmUp(paths, () => Interlocked.Increment(ref warmed));

            Assert.Equal(3, warmed);
        }

        [Fact]
        public void WarmUp_MissingFile_DoesNotThrow()
        {
            var path = Path.Combine(directory, "does-not-exist.ogg");

            var exception = Record.Exception(() => AudioFileCache.WarmUp([path]));

            Assert.Null(exception);
        }

        [Fact]
        public void GetFormat_AfterFailedWarmUp_StillThrowsForChecksToReport()
        {
            var path = CreateBogusOgg("soft-sliderslide.ogg");

            AudioFileCache.WarmUp([path]);

            // Checks rely on this to raise their own "Exception" issue for the file.
            Assert.ThrowsAny<Exception>(() => AudioFileCache.GetFormat(path));
        }
    }
}
