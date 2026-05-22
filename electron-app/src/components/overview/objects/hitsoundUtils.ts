import type { ObjectsTimelineSample } from '../../../Types';

export const HITSOUND_FLAG_NORMAL = 1;
export const HITSOUND_FLAG_WHISTLE = 2;
export const HITSOUND_FLAG_FINISH = 4;
export const HITSOUND_FLAG_CLAP = 8;

export const HITSOUND_COLORS = {
  normal: '#6b7280',
  whistle: '#38bdf8',
  clap: '#4ade80',
  finish: '#fbbf24',
  body: '#52525b',
  tick: '#71717a',
} as const;

export const SAMPLESET_TINTS: Record<string, string> = {
  Normal: '#71717a',
  Soft: '#fbbf24',
  Drum: '#38bdf8',
  Auto: '#71717a',
};

export const SAMPLESET_OVERLAY_ALPHA = 0.07;
export const SAMPLESET_ACCENT_ALPHA = 0.45;

export type HitsoundLayerVisibility = {
  body: boolean;
  ticks: boolean;
  sampleset: boolean;
  gaps: boolean;
};

export const DEFAULT_HITSOUND_LAYERS: HitsoundLayerVisibility = {
  body: true,
  ticks: true,
  sampleset: true,
  gaps: true,
};

export type TimelineViewMode = 'structure' | 'hitsounding';

export function getDominantHitsoundColor(flags: number): string {
  if (flags & HITSOUND_FLAG_FINISH) return HITSOUND_COLORS.finish;
  if (flags & HITSOUND_FLAG_CLAP) return HITSOUND_COLORS.clap;
  if (flags & HITSOUND_FLAG_WHISTLE) return HITSOUND_COLORS.whistle;
  if (flags & HITSOUND_FLAG_NORMAL) return HITSOUND_COLORS.normal;
  return HITSOUND_COLORS.normal;
}

export function getSecondaryHitsoundColor(flags: number): string | null {
  const primary =
    flags & HITSOUND_FLAG_FINISH
      ? HITSOUND_FLAG_FINISH
      : flags & HITSOUND_FLAG_CLAP
        ? HITSOUND_FLAG_CLAP
        : flags & HITSOUND_FLAG_WHISTLE
          ? HITSOUND_FLAG_WHISTLE
          : flags & HITSOUND_FLAG_NORMAL
            ? HITSOUND_FLAG_NORMAL
            : 0;

  const remaining = flags & ~primary;
  if (remaining === 0) return null;
  return getDominantHitsoundColor(remaining);
}

export function getSampleColor(hitSound: string | null, source?: string): string {
  if (!hitSound) {
    if (source === 'Tick') {
      return HITSOUND_COLORS.tick;
    }
    return HITSOUND_COLORS.body;
  }

  return getDominantHitsoundColor(parseHitSoundFlags(hitSound));
}

export function parseHitSoundFlags(hitSound: string): number {
  let flags = 0;

  if (hitSound.includes('Normal')) {
    flags |= HITSOUND_FLAG_NORMAL;
  }
  if (hitSound.includes('Whistle')) {
    flags |= HITSOUND_FLAG_WHISTLE;
  }
  if (hitSound.includes('Finish')) {
    flags |= HITSOUND_FLAG_FINISH;
  }
  if (hitSound.includes('Clap')) {
    flags |= HITSOUND_FLAG_CLAP;
  }

  return flags;
}

export type HitsoundTypeDisplay = {
  label: string;
  color: string;
  role: 'fill' | 'ring';
};

const HITSOUND_TYPE_ORDER = [
  { flag: HITSOUND_FLAG_FINISH, label: 'Finish', color: HITSOUND_COLORS.finish },
  { flag: HITSOUND_FLAG_CLAP, label: 'Clap', color: HITSOUND_COLORS.clap },
  { flag: HITSOUND_FLAG_WHISTLE, label: 'Whistle', color: HITSOUND_COLORS.whistle },
  { flag: HITSOUND_FLAG_NORMAL, label: 'Normal', color: HITSOUND_COLORS.normal },
] as const;

export function getHitsoundTypesFromFlags(flags: number): HitsoundTypeDisplay[] {
  const active = HITSOUND_TYPE_ORDER.filter((type) => flags & type.flag);
  if (active.length === 0) {
    return [{ label: 'Normal', color: HITSOUND_COLORS.normal, role: 'fill' }];
  }

  return active.map((type, index) => ({
    label: type.label,
    color: type.color,
    role: index === 0 ? 'fill' : 'ring',
  }));
}

export function getHitsoundTypesFromSample(hitSound: string | null): HitsoundTypeDisplay[] {
  const flags = parseHitSoundFlags(hitSound ?? '');
  return getHitsoundTypesFromFlags(flags || HITSOUND_FLAG_NORMAL);
}

export function formatSampleBankLine(sampleset: string, customIndex: number): string {
  const bank = sampleset || 'Normal';
  return customIndex > 1 ? `${bank} · #${customIndex}` : bank;
}

export function formatSampleLabel(sample: ObjectsTimelineSample): string {
  const parts = [
    sample.partName,
    sample.hitSound,
    sample.sampleset,
    sample.customIndex > 1 ? `#${sample.customIndex}` : null,
  ].filter(Boolean);
  return parts.join(' · ');
}

export function findNearestSample(
  samples: ObjectsTimelineSample[],
  timestampMs: number,
  toleranceMs = 15
): ObjectsTimelineSample | null {
  let best: ObjectsTimelineSample | null = null;
  let bestDistance = Number.POSITIVE_INFINITY;

  for (const sample of samples) {
    const distance = Math.abs(sample.timeMs - timestampMs);
    if (distance > toleranceMs) continue;
    if (distance < bestDistance) {
      bestDistance = distance;
      best = sample;
    }
  }

  return best;
}

export function isHitsoundViewAvailable(mode: string | undefined): boolean {
  return mode === 'Standard' || mode === 'Catch';
}
