import type { Mode } from '../Types';

const LABELS: Record<Mode, string> = {
  Standard: 'osu!',
  Taiko: 'osu!taiko',
  Catch: 'osu!catch',
  Mania: 'osu!mania',
};

export function formatGameModeLabel(mode: Mode | string): string {
  if (mode in LABELS) {
    return LABELS[mode as Mode];
  }
  return String(mode);
}
