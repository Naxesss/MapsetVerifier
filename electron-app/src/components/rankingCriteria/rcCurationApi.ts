import { QueryClient } from '@tanstack/react-query';
import { apiFetch } from '../../client/ApiHelper';
import { ApiRcStatement, RcAutomation } from '../../Types';

/**
 * Dev-only calls that write to the catalogue in MapsetVerifier.RankingCriteria/Data. Kept apart from
 * RankingCriteriaApi since the endpoints only exist on a Debug backend; callers sit behind
 * isDevBuild so production bundles leave them out.
 */
async function send(path: string, method: string, body: unknown) {
  const res = await apiFetch(path, {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const message = await res.text();
    // A Release backend has no such route, so it answers without a message of its own.
    throw new Error(
      message ||
        (res.status === 404 || res.status === 405
          ? 'The backend has no curation endpoint. Run a Debug build of the backend.'
          : `Saving failed (${res.status})`)
    );
  }

  return (await res.json()) as ApiRcStatement;
}

/** Writes a statement's curation to its catalogue file. */
export function updateCuration(curation: {
  id: string;
  automation: RcAutomation;
  notes: string | null;
}) {
  return send('/ranking-criteria/curation', 'PUT', curation);
}

/** Marks a statement's checks as reviewed against its current wiki wording. */
export function markReviewed(id: string) {
  return send('/ranking-criteria/curation/review', 'POST', { id });
}

/** Refetches everything showing statements after one changed. */
export function invalidateRankingCriteria(queryClient: QueryClient) {
  return Promise.all(
    ['rankingCriteriaPage', 'rankingCriteriaOverview', 'rankingCriteriaStatements'].map((key) =>
      queryClient.invalidateQueries({ queryKey: [key] })
    )
  );
}
