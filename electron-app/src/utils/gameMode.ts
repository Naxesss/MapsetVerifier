import type { Mode } from '../Types';

const LABELS: Record<Mode, string> = {
  Standard: 'osu!',
  Taiko: 'osu!taiko',
  Catch: 'osu!catch',
  Mania: 'osu!mania',
};

export const MODE_ORDER: Mode[] = ['Standard', 'Taiko', 'Catch', 'Mania'];

export function normalizeMode(mode: string): Mode {
  return MODE_ORDER.includes(mode as Mode) ? (mode as Mode) : 'Standard';
}

/** Splits difficulties by game mode, in the usual mode order and without the modes that have none. */
export function groupByMode<T extends { mode: string }>(difficulties: readonly T[]) {
  return MODE_ORDER.map((mode) => ({
    mode,
    difficulties: difficulties.filter((d) => normalizeMode(d.mode) === mode),
  })).filter((group) => group.difficulties.length > 0);
}

export function formatGameModeLabel(mode: Mode | string): string {
  if (mode in LABELS) {
    return LABELS[mode as Mode];
  }
  return String(mode);
}

/** Mantine palette index 4 as CSS variables (no theme hook). */
export function getModeAccentColor(mode: Mode | string): string {
  switch (mode) {
    case 'Standard':
      return 'var(--mantine-color-pink-4)';
    case 'Taiko':
      return 'var(--mantine-color-green-4)';
    case 'Catch':
      return 'var(--mantine-color-blue-4)';
    case 'Mania':
      return 'var(--mantine-color-violet-4)';
    default:
      return 'var(--mantine-color-gray-4)';
  }
}
