#if DEBUG
using System.Text.Json;
using MapsetVerifier.RankingCriteria;
using MapsetVerifier.RankingCriteria.Model;
using MapsetVerifier.Server.Model;

namespace MapsetVerifier.Server.Service;

/// <summary>
///     Writes curation decisions back to the catalogue in the source checkout, so they can be committed. Debug builds
///     only: a release build has no checkout to write to.
/// </summary>
public static partial class RankingCriteriaService
{
    private static readonly Lock CurationLock = new();

    public static ApiRcStatement? UpdateCuration(string id, RcAutomation automation, string? notes)
    {
        if (!Store.TryGetStatement(id, out var page, out var statement))
            return null;

        var curation = new RcCuration
        {
            Automation = automation,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
        };

        lock (CurationLock)
        {
            // Read the file rather than serializing the embedded copy, which may be older than the checkout.
            var path = Path.Combine(RcDataDirectory.Find(), "catalogue", page.Key + ".json");
            var catalogue = JsonSerializer.Deserialize<RcCataloguePage>(
                File.ReadAllText(path),
                RcJson.Options
            )!;
            var onDisk =
                catalogue.Statements.FirstOrDefault(other => other.Id == id)
                ?? throw new InvalidOperationException(
                    $"{id} is not in {path}. Rebuild after syncing the ranking criteria."
                );

            onDisk.Curation = curation;
            File.WriteAllText(path, JsonSerializer.Serialize(catalogue, RcJson.Options) + "\n");

            // Also update the embedded copy, so the change shows without a rebuild.
            statement.Curation = curation;
        }

        return ToApi(page, statement, GetLinks());
    }
}
#endif
