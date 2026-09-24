using MapsetVerifier.RankingCriteria.Parsing;
using Xunit;

namespace MapsetVerifier.RankingCriteria.Tests;

/// <summary> Guards the embedded snapshot and catalogue against getting out of sync with each other. </summary>
public class RcStoreTests
{
    [Fact]
    public void EmbeddedSnapshotContainsEveryPage()
    {
        var store = RcStore.Embedded;

        Assert.NotEmpty(store.Source.Commit);
        foreach (var page in RcPages.All)
            Assert.False(string.IsNullOrEmpty(store.GetMarkdown(page.Key)), page.Key);
    }

    [Fact]
    public void CatalogueMatchesTheSnapshot()
    {
        var store = RcStore.Embedded;

        foreach (var page in RcPages.All.Where(page => page.HasStatements))
        {
            var parsed = RcMarkdownParser.Parse(store.GetMarkdown(page.Key)!);
            var active = store
                .GetStatements(page.Key)
                .Where(statement => !statement.IsRetired)
                .ToList();

            // Running the parser on the snapshot must reproduce the catalogue; otherwise the tool needs a reparse.
            Assert.Equal(parsed.Count, active.Count);
            for (var i = 0; i < parsed.Count; ++i)
            {
                Assert.Equal(parsed[i].Upstream.Fingerprint, active[i].Upstream.Fingerprint);
                Assert.Equal(parsed[i].Upstream.StartLine, active[i].Upstream.StartLine);
            }
        }
    }

    [Fact]
    public void GeneratedConstantsExistInTheCatalogue()
    {
        var constants = typeof(RC)
            .GetNestedTypes()
            .SelectMany(type => type.GetFields())
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(constants);
        foreach (var id in constants)
        {
            Assert.True(RcStore.Embedded.TryGetStatement(id, out _, out var statement), id);
            Assert.False(statement.IsRetired, id);
        }
    }
}
