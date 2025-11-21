using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Statics;
using Xunit;
using System.IO;
using System;

namespace MapsetVerifier.Parser.Tests.Statics
{
    public class SkinStaticTests
    {
        private BeatmapSet CreateBeatmapSet(string osuCode, string[]? extraFiles = null)
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "MapsetVerifierTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempRoot);
            File.WriteAllText(Path.Combine(tempRoot, "TestMap.osu"), osuCode);
            if (extraFiles != null)
                foreach (var file in extraFiles)
                    File.WriteAllText(Path.Combine(tempRoot, file), "");
            return new BeatmapSet(tempRoot);
        }
        private string BuildOsu(Beatmap.Mode mode, string hitObjectsSection = "", int countdown = 0, bool includeBreak = false) => string.Join("\n", new[]
        {
            "osu file format v14",
            "[General]",
            "AudioFilename: audio.mp3",
            "Mode: " + (int)mode,
            "Countdown: " + countdown,
            "[Metadata]",
            "Title:Title",
            "Artist:Artist",
            "Creator:Creator",
            "Version:Diff",
            "[Difficulty]",
            "StackLeniency:0.7",
            "[Events]",
            includeBreak ? "2,0,4000" : "",
            "[TimingPoints]",
            "0,500,4,2,0,100,1,0",
            "[HitObjects]",
            hitObjectsSection
        });

        [Fact]
        public void GeneralElement_AlwaysUsed()
        {
            var set = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Standard));
            Assert.True(SkinStatic.IsUsed("cursor.png", set));
        }

        [Fact]
        public void StandardElement_NotUsedInManiaOnlySet()
        {
            var set = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Mania));
            Assert.False(SkinStatic.IsUsed("hitcircle.png", set));
        }

        [Fact]
        public void ManiaElement_UsedInManiaSet()
        {
            var set = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Mania));
            Assert.True(SkinStatic.IsUsed("mania-hit300.png", set));
        }

        [Fact]
        public void SliderElements_RequireSlider()
        {
            var setNoSlider = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Standard, "256,192,1000,1,0,0:0:0:0:"));
            Assert.False(SkinStatic.IsUsed("sliderb.png", setNoSlider));

            var sliderLine = "256,192,1000,2,0,L|300:192,1,100";
            var setWithSlider = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Standard, sliderLine));
            Assert.True(SkinStatic.IsUsed("sliderb.png", setWithSlider));
        }

        [Fact]
        public void SpinnerElements_RequireSpinner()
        {
            var spinnerLine = "256,192,2000,8,0,3000,0:0:0:0:";
            var setSpinner = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Standard, spinnerLine));
            Assert.True(SkinStatic.IsUsed("spinner-approachcircle.png", setSpinner));

            var setNoSpinner = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Standard, "256,192,1000,1,0,0:0:0:0:"));
            Assert.False(SkinStatic.IsUsed("spinner-approachcircle.png", setNoSpinner));
        }

        [Fact]
        public void ReverseArrow_RequiresRepeatSlider()
        {
            var noRepeat = "256,192,1000,2,0,L|300:192,1,100";
            Assert.False(SkinStatic.IsUsed("reversearrow.png", CreateBeatmapSet(BuildOsu(Beatmap.Mode.Standard, noRepeat))));

            var repeat = "256,192,1000,2,0,L|300:192,2,100";
            Assert.True(SkinStatic.IsUsed("reversearrow.png", CreateBeatmapSet(BuildOsu(Beatmap.Mode.Standard, repeat))));
        }

        [Fact]
        public void CountdownElements_RequireCountdown()
        {
            var setCountdown = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Standard, "256,192,1000,1,0,0:0:0:0:", countdown: 1));
            Assert.True(SkinStatic.IsUsed("count1.png", setCountdown));

            var setNoCountdown = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Standard, "256,192,1000,1,0,0:0:0:0:"));
            Assert.False(SkinStatic.IsUsed("count1.png", setNoCountdown));
        }

        [Fact]
        public void BreakElements_RequireBreakEvent()
        {
            var setBreak = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Standard, "256,192,1000,1,0,0:0:0:0:", includeBreak: true));
            Assert.True(SkinStatic.IsUsed("section-pass.png", setBreak));

            var setNoBreak = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Standard, "256,192,1000,1,0,0:0:0:0:"));
            Assert.False(SkinStatic.IsUsed("section-pass.png", setNoBreak));
        }

        [Fact]
        public void AnimationFramePattern_Recognized()
        {
            var set = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Standard, "256,192,1000,1,0,0:0:0:0:"));
            Assert.True(SkinStatic.IsUsed("followpoint-2.png", set));
        }

        [Fact]
        public void Followpoint_BaseElementAlwaysUsedDespiteAnimationFrame()
        {
            var setWithFrame = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Standard, "256,192,1000,1,0,0:0:0:0:"), new[] { "followpoint-0.png" });
            Assert.True(SkinStatic.IsUsed("followpoint.png", setWithFrame));
        }

        [Fact]
        public void SliderbNdElementUsedRegardlessOfSliderbPresence()
        {
            var sliderLine = "256,192,1000,2,0,L|300:192,1,100";
            var setNoSliderb = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Standard, sliderLine));
            Assert.True(SkinStatic.IsUsed("sliderb-nd.png", setNoSliderb));

            var setWithSliderb = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Standard, sliderLine), new[] { "sliderb.png" });
            Assert.True(SkinStatic.IsUsed("sliderb-nd.png", setWithSliderb));
        }

        [Fact]
        public void UnknownElement_NotUsed()
        {
            var set = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Standard));
            Assert.False(SkinStatic.IsUsed("nonexistent-element.png", set));
        }

        [Fact]
        public void ManiaComboBurstAnimationFrames_Used()
        {
            var set = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Mania));
            // Only mania combo burst frames are used in mania mode
            Assert.False(SkinStatic.IsUsed("comboburst-0.png", set));
            Assert.False(SkinStatic.IsUsed("comboburst-1.png", set));
            Assert.False(SkinStatic.IsUsed("comboburst-catch-0.png", set));
            Assert.False(SkinStatic.IsUsed("comboburst-catch-1.png", set));
            
            Assert.True(SkinStatic.IsUsed("comboburst-mania-0.png", set));
            Assert.True(SkinStatic.IsUsed("comboburst-mania-1.png", set));
        }

        [Fact]
        public void ManiaComboBurstStillFrame_AlwaysUsedRegardlessOfFrames()
        {
            var setWithFrame = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Mania), new[] { "comboburst-mania-0.png" });
            Assert.True(SkinStatic.IsUsed("comboburst-mania.png", setWithFrame));

            var setNoFrame = CreateBeatmapSet(BuildOsu(Beatmap.Mode.Mania));
            Assert.True(SkinStatic.IsUsed("comboburst-mania.png", setNoFrame));
        }
    }
}
