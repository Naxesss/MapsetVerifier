import type { ApiSnapshotHistory, ApiSnapshotResult } from '../../Types';

export function getSnapshotHistory(
  data: ApiSnapshotResult,
  selectedDifficulty?: string
): ApiSnapshotHistory | null {
  if (selectedDifficulty === 'General' && data.general) {
    return data.general;
  }
  if (!selectedDifficulty || selectedDifficulty === 'General') {
    return null;
  }
  return data.beatmapHistories.find((h) => h.difficultyName === selectedDifficulty) ?? null;
}

function commitHasChanges(commits: ApiSnapshotHistory['commits'], commitId: string | undefined) {
  if (!commitId) {
    return false;
  }
  const commit = commits.find((c) => c.id === commitId);
  return (commit?.totalChanges ?? 0) > 0;
}

/** Whether the general (beatmapset) snapshot had changes at the given commit. */
export function generalHasChangesAtCommit(data: ApiSnapshotResult, commitId: string | undefined) {
  return commitHasChanges(data.general?.commits ?? [], commitId);
}

/** Whether this beatmap difficulty had changes at the given snapshot (global timeline commit id). */
export function difficultyHasChangesAtCommit(
  data: ApiSnapshotResult,
  difficultyName: string,
  commitId: string | undefined
) {
  const history = data.beatmapHistories.find((h) => h.difficultyName === difficultyName);
  return commitHasChanges(history?.commits ?? [], commitId);
}

/** Whether this beatmap difficulty has any snapshot history at all. */
export function difficultyHasSnapshot(data: ApiSnapshotResult, difficultyName: string) {
  const history = data.beatmapHistories.find((h) => h.difficultyName === difficultyName);
  return !!history?.commits.length;
}
