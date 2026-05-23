import type { ApiSnapshotHistory, ApiSnapshotResult } from '../../Types';

export function getSnapshotHistory(
  data: ApiSnapshotResult,
  selectedDifficulty?: string,
): ApiSnapshotHistory | null {
  if (selectedDifficulty === 'General' && data.general) {
    return data.general;
  }
  if (!selectedDifficulty || selectedDifficulty === 'General') {
    return null;
  }
  return data.beatmapHistories.find((h) => h.difficultyName === selectedDifficulty) ?? null;
}

function commitHasChanges(
  commits: ApiSnapshotHistory['commits'],
  commitId: string | undefined,
) {
  if (!commitId) {
    return false;
  }
  const commit = commits.find((c) => c.id === commitId);
  return (commit?.totalChanges ?? 0) > 0;
}

function historyHasAnyChanges(commits: ApiSnapshotHistory['commits']) {
  return commits.some((commit) => commit.totalChanges > 0);
}

function findCommitDate(data: ApiSnapshotResult, commitId: string): string | undefined {
  const generalCommit = data.general?.commits.find((c) => c.id === commitId);
  if (generalCommit) {
    return generalCommit.date;
  }

  for (const history of data.beatmapHistories) {
    const commit = history.commits.find((c) => c.id === commitId);
    if (commit) {
      return commit.date;
    }
  }

  return undefined;
}

function findClosestCommitId(
  commits: ApiSnapshotHistory['commits'],
  targetDate: string,
): string | undefined {
  if (commits.length === 0) {
    return undefined;
  }

  const targetMs = Date.parse(targetDate);
  let closest = commits[0];
  let closestDistance = Math.abs(Date.parse(closest.date) - targetMs);

  for (const commit of commits.slice(1)) {
    const distance = Math.abs(Date.parse(commit.date) - targetMs);
    if (distance < closestDistance) {
      closest = commit;
      closestDistance = distance;
    }
  }

  return closest.id;
}

/** Resolve a commit id for a history, preserving timeline position when switching difficulties. */
export function resolveSnapshotCommitId(
  data: ApiSnapshotResult,
  history: ApiSnapshotHistory | null,
  currentCommitId: string | undefined,
): string | undefined {
  if (!history?.commits.length) {
    return undefined;
  }

  if (currentCommitId && history.commits.some((commit) => commit.id === currentCommitId)) {
    return currentCommitId;
  }

  if (currentCommitId) {
    const currentDate = findCommitDate(data, currentCommitId);
    if (currentDate) {
      return findClosestCommitId(history.commits, currentDate) ?? history.commits[0].id;
    }
  }

  return history.commits[0].id;
}

/** Whether the general (beatmapset) snapshot ever had changes. */
export function generalHasAnyChanges(data: ApiSnapshotResult) {
  return historyHasAnyChanges(data.general?.commits ?? []);
}

/** Whether this beatmap difficulty ever had changes in snapshot history. */
export function difficultyHasAnyChanges(data: ApiSnapshotResult, difficultyName: string) {
  const history = data.beatmapHistories.find((h) => h.difficultyName === difficultyName);
  return historyHasAnyChanges(history?.commits ?? []);
}

/** Whether the general (beatmapset) snapshot had changes at the given commit. */
export function generalHasChangesAtCommit(
  data: ApiSnapshotResult,
  commitId: string | undefined,
) {
  return commitHasChanges(data.general?.commits ?? [], commitId);
}

/** Whether this beatmap difficulty had changes at the given snapshot (global timeline commit id). */
export function difficultyHasChangesAtCommit(
  data: ApiSnapshotResult,
  difficultyName: string,
  commitId: string | undefined,
) {
  const history = data.beatmapHistories.find((h) => h.difficultyName === difficultyName);
  return commitHasChanges(history?.commits ?? [], commitId);
}
