namespace MapsetVerifier.Checks.Tests;

/// <summary> Mapsets with a single hit sound file which is in use. </summary>
internal static class HitSoundContext
{
    /// <summary> Soft sampleset with custom index 2 throughout, on a circle at 1000 ms unless given others. </summary>
    public static CheckTestContext Create(
        byte[] file,
        string fileName = "soft-hitnormal2.wav",
        IEnumerable<string>? hitObjects = null
    ) =>
        CheckTestContext.CreateFromOsuFiles(
            [
                (
                    "test.osu",
                    new OsuBuilder()
                        .TimingPoints(
                            TestTimingPoints.Uninherited(0, 500, sampleSet: 2, sampleIndex: 2)
                        )
                        .HitObjects(hitObjects ?? [TestHitObjects.Circle(1000)])
                        .Build()
                ),
            ],
            extraBinaryFiles: [(fileName, file)]
        );
}
