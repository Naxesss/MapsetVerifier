using MapsetVerifier.Checks.Catch.Compose.Platter;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using Xunit;

namespace MapsetVerifier.Checks.Tests.Catch.Compose.Platter;

public class CheckPlatterHigherSnappedHyperdashTests
{
    private static List<Issue> Run(
        int hyperMs,
        CatchNoteDirection walkDirection,
        int walkDistance,
        CatchMovementType next = CatchMovementType.Walk
    )
    {
        using var context = CheckTestContext.CreateFromOsu(
            new OsuBuilder()
                .Mode(Beatmap.Mode.Catch)
                .WithDefaultTiming()
                .HitObjects(
                    TestHitObjects.Circle(1000, 50),
                    TestHitObjects.Circle(1000 + hyperMs, 350),
                    TestHitObjects.Circle(1000 + hyperMs + 200, 350 - walkDistance)
                )
        );
        CatchObjects.Set(context, CatchMovementType.Hyperdash, next);

        var objects = context.GetBeatmap("Test").HitObjects.Cast<ICatchHitObject>().ToList();
        objects[0].NoteDirection = CatchNoteDirection.Right;
        objects[1].NoteDirection = walkDirection;

        return context.RunBeatmapCheck<CheckPlatterHigherSnappedHyperdash>("Test");
    }

    [Fact]
    public void Antiflow_AfterAHigherSnappedHyperdash_IsAWarning()
    {
        var issue = Assert.Single(Run(200, CatchNoteDirection.Left, 60));

        Assert.Equal(Issue.Level.Warning, issue.level);
        Assert.True(issue.AppliesToDifficulty(Beatmap.Difficulty.Hard));
    }

    [Fact]
    public void Otherwise_ItIsFine()
    {
        Assert.Empty(Run(300, CatchNoteDirection.Left, 60)); // Basic-snapped.
        Assert.Empty(Run(200, CatchNoteDirection.Right, 60)); // Same direction.
        Assert.Empty(Run(200, CatchNoteDirection.None, 0));
        Assert.Empty(Run(200, CatchNoteDirection.Left, 40)); // Hardly moves in 200 ms.
        Assert.Empty(Run(200, CatchNoteDirection.Left, 60, CatchMovementType.Dash));
    }
}
