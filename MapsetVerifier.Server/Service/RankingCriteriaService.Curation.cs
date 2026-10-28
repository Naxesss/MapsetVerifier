#if DEBUG
using MapsetVerifier.RankingCriteria;
using MapsetVerifier.RankingCriteria.Model;
using MapsetVerifier.RankingCriteria.Sync;
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
            var dataDir = RcDataDirectory.Find();
            var catalogue = RcCatalogueFiles.ReadPage(dataDir, page.Key);
            var onDisk =
                catalogue.Statements.FirstOrDefault(other => other.Id == id)
                ?? throw new InvalidOperationException(
                    $"{id} is not in {RcCatalogueFiles.PagePath(dataDir, page.Key)}. Rebuild after syncing the ranking criteria."
                );

            onDisk.Curation = curation;
            RcCatalogueFiles.WritePage(dataDir, catalogue);

            // Also update the embedded copy, so the change shows without a rebuild.
            statement.Curation = curation;
        }

        return ToApi(page, statement, GetLinks());
    }

    /// <summary>
    ///     Marks a statement's checks as reviewed against its current wording, like the tool's <c>review</c> command.
    ///     The obsolete warning on its RC constant goes away on the next build.
    /// </summary>
    public static ApiRcStatement? MarkReviewed(string id)
    {
        if (!Store.TryGetStatement(id, out var page, out var statement))
            return null;

        lock (CurationLock)
        {
            // Already reviewed on disk counts too, e.g. through the tool since this build.
            RcCatalogueFiles.MarkReviewed(RcDataDirectory.Find(), [id]);
            statement.LastReview = null;
        }

        return ToApi(page, statement, GetLinks());
    }
}
#endif
