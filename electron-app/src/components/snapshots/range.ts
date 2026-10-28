import type { ApiSnapshotHistoryEntry } from '../../Types';

/**
 * Which snapshot a comparison starts from, relative to the one it ends at: the one right before,
 * the last one that was pinned as a milestone, the very first, or one picked by hand.
 */
export type RangePreset = 'previous' | 'pin' | 'all' | 'custom';

/**
 * The snapshot to compare from. `entries` is newest first, so "older" means a higher index than
 * the target. Returns undefined when nothing is older than the target (the first snapshot).
 */
export function resolveBaseId(
  entries: ApiSnapshotHistoryEntry[],
  targetId: string | undefined,
  preset: RangePreset,
  customBaseId?: string
): string | undefined {
  const targetIndex = entries.findIndex((e) => e.id === targetId);
  if (targetIndex < 0) return undefined;

  const older = entries.slice(targetIndex + 1);
  if (older.length === 0) return undefined;

  switch (preset) {
    case 'all':
      return older[older.length - 1].id;
    case 'pin':
      return (older.find((e) => e.pin) ?? older[older.length - 1]).id;
    case 'custom':
      return older.find((e) => e.id === customBaseId)?.id ?? older[0].id;
    default:
      return older[0].id;
  }
}

/** Index of a snapshot in the (newest first) list, or -1. */
export function indexOfEntry(entries: ApiSnapshotHistoryEntry[], id?: string) {
  return id ? entries.findIndex((e) => e.id === id) : -1;
}
