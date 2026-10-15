using MapsetVerifier.Parser.Objects.HitObjects.Catch;
using Xunit;

namespace MapsetVerifier.Parser.Tests.Objects.HitObjects.Catch;

/// <summary>
/// Calibration tests against real difficulties where the mapper bookmarked every movement of the kind
/// that difficulty is about. This is the only ground truth for where the walk, dash and hyperdash
/// boundaries actually belong: <see cref="HitObjectDistanceCalculatorTests"/> pins the thresholds
/// down, but cannot tell whether they sit in the right place.
///
/// Salads bookmark dashes, since a Salad may not contain hyperdashes at all. Platters bookmark
/// hyperdashes, the feature that difficulty introduces, and leave their dashes unmarked.
///
/// A bookmark marks the object the player lands on, while <see cref="ICatchHitObject.MovementType"/>
/// describes the movement leaving an object, so the classification sits on the object before a
/// bookmark. Bookmarks are matched with a 12ms tolerance because some were placed before an offset
/// change, shifting all of them by a constant amount of up to 10ms.
///
/// Across these maps 516 bookmarks agree with 508 detected dashes. The disagreements are listed per
/// map rather than tolerated by a threshold, so that any change to the calculator has to state what
/// it did to them. They fall into two groups:
///
/// - Bookmarks that cannot be a dash under any model, marking something else the mapper cared about.
///   All of these sit far below walking speed, several start from a spinner.
/// - Detected dashes with no bookmark, all at 0.76px/ms or above where the catcher walks 0.5px/ms.
///   These are bookmarks the mapper missed rather than misclassifications.
/// </summary>
public class BookmarkedDashTests
{
    /// <summary>fixture, difficulty, bookmarks, bookmarks that are not dashes, dashes not bookmarked.</summary>
    public static TheoryData<string, string, int, int[], int[]> Salads =>
        new()
        {
            { "CatchBookmarksOmoideShiritori", "Salad", 24, [], [] },
            { "CatchBookmarksHarutoki", "Salad", 26, [], [] },
            { "CatchBookmarksHaiiroNoSaga", "Salad", 45, [], [] },
            { "CatchBookmarksAshitaNoHanatachi", "Salad", 42, [], [] },
            // 0.44px/ms, half the spacing of every other bookmark in the map.
            { "CatchBookmarksEverythingEverything", "Greaper's Salad", 34, [77993], [99081] },
            // 2.6px short of the walking range.
            { "CatchBookmarksAdrenaline", "Salad", 51, [82229], [33870] },
            // 52928 has no object near it, the other two are 0.18px/ms and 0.44px/ms.
            { "CatchBookmarksHanabiraMemories", "Salad", 49, [52928, 54498, 93568], [132637] },
            // 17.7px short of the walking range.
            { "CatchBookmarksKillyKillyJoker", "Salad", 35, [25331], [] },
            // 2.4px short of the walking range.
            { "CatchBookmarksNowLoading", "Salad", 32, [63956], [22764] },
            // 5.4px short of the walking range.
            { "CatchBookmarksKissNoHitotsuDe", "Salad", 41, [97344], [] },
            // 5.2px and 4.1px short of the walking range, 78047 is 0.38px/ms.
            { "CatchBookmarksGiriGiri", "Salad", 44, [34219, 41719, 78047], [] },
            // Two start from a spinner at under 0.1px/ms, 59066 is 15.1px short.
            { "CatchBookmarksPleasePlease", "Salad", 53, [34066, 50066, 59066], [46056] },
            { "CatchBookmarksHanaNiNatte", "Salad", 40, [], [25100] },
        };

    public static TheoryData<string, string> SaladFixtures
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var row in Salads)
                data.Add((string)row[0], (string)row[1]);

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Salads))]
    public void SaladDashesMatchBookmarks(
        string fixture,
        string version,
        int expectedBookmarks,
        int[] bookmarksThatAreNotDashes,
        int[] dashesWithoutBookmark
    )
    {
        var (bookmarks, landings) = Analyse(fixture, version, CatchMovementType.Dash);

        Assert.Equal(expectedBookmarks, bookmarks.Count);
        Assert.Equal(bookmarksThatAreNotDashes, Unmatched(bookmarks, landings));
        Assert.Equal(dashesWithoutBookmark, Unmatched(landings, bookmarks));
    }

    [Theory]
    [MemberData(nameof(SaladFixtures))]
    public void SaladsHaveNoHyperdashes(string fixture, string version)
    {
        Assert.DoesNotContain(
            CatchTestBeatmap.Load(fixture, version).GetCatchHitObjects(true),
            o => o.MovementType == CatchMovementType.Hyperdash
        );
    }

    [Theory]
    [InlineData("CatchBookmarksOmoideShiritori", "Platter", 27, 0)]
    [InlineData("CatchBookmarksEverythingEverything", "Greaper's Platter", 28, 1)]
    public void PlatterHyperdashesMatchBookmarks(
        string fixture,
        string version,
        int expectedBookmarks,
        int allowedUnexplained
    )
    {
        var (bookmarks, landings) = Analyse(fixture, version, CatchMovementType.Hyperdash);

        Assert.Equal(expectedBookmarks, bookmarks.Count);

        // Hyperdash detection mirrors the game exactly, so every bookmark should be accounted for.
        Assert.Equal(allowedUnexplained, Unmatched(bookmarks, landings).Length);
    }

    private const double BookmarkTolerance = 12;

    /// <summary>Times in the first list that have no counterpart in the second.</summary>
    private static int[] Unmatched(List<double> times, List<double> other) =>
        times
            .Where(time => !other.Any(o => Math.Abs(o - time) <= BookmarkTolerance))
            .Select(time => (int)time)
            .ToArray();

    /// <summary>
    /// Returns the mapper's bookmarks and the times of the objects each detected movement lands on.
    /// </summary>
    private static (List<double> Bookmarks, List<double> Landings) Analyse(
        string fixture,
        string version,
        CatchMovementType movementType
    )
    {
        var objects = CatchTestBeatmap.Load(fixture, version).GetCatchHitObjects(true);

        var landings = objects
            .Select((o, index) => (o, index))
            .Where(x => x.o.MovementType == movementType && x.index + 1 < objects.Count)
            .Select(x => objects[x.index + 1].Time)
            .ToList();

        return (CatchTestBeatmap.ReadBookmarks(fixture, version), landings);
    }
}
