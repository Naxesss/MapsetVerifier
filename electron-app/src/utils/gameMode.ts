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
