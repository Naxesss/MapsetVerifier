import { ApiSnapshotComparison, ApiSnapshotHistory, ApiSnapshotWindow } from '../Types.ts';
import { apiFetch, FetchError } from './ApiHelper.ts';

async function post<T>(path: string, body: unknown): Promise<T> {
  const response = await apiFetch(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });

  const raw = await response.text();
  let data: any = undefined;
  try {
    data = raw ? JSON.parse(raw) : undefined;
  } catch {
    /* ignore parse errors */
  }

  if (response.ok) {
    return data as T;
  }

  const message = data?.message || data?.error || raw || `HTTP ${response.status}`;
  throw new FetchError(response, message, data?.stackTrace);
}

const SnapshotApi = {
  /** Takes a snapshot of the mapset as it is now, then lists all its snapshots. */
  getHistory: (folder: string) => post<ApiSnapshotHistory>('/snapshot/history', { folder }),

  compare: (setKey: string, baseId: string, targetId: string) =>
    post<ApiSnapshotComparison>('/snapshot/compare', { setKey, baseId, targetId }),

  /** A few objects of one difficulty around a change, before and after. */
  window: (
    setKey: string,
    baseId: string,
    targetId: string,
    difficultyKey: string,
    anchor: number,
    anchorEnd: number,
    offset: number
  ) =>
    post<ApiSnapshotWindow>('/snapshot/window', {
      setKey,
      baseId,
      targetId,
      difficultyKey,
      anchor,
      anchorEnd,
      offset,
    }),

  /** Names a snapshot as a milestone, or clears the name. */
  setPin: (setKey: string, id: string, pin: string | null) =>
    post<ApiSnapshotHistory>('/snapshot/pin', { setKey, id, pin }),
};

export default SnapshotApi;
