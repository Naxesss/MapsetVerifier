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

export const HITSOUND_RING_BASE_OFFSET = 1.5;
export const HITSOUND_RING_STEP = 2;

export type HitsoundCircleLayout = {
  fillRadius: number;
  ringLineWidth: number;
  ringBaseOffset: number;
  ringStep: number;
  outerRadius: number;
};

function getOrderedActiveHitsoundTypes(flags: number) {
  let active = HITSOUND_TYPE_ORDER.filter((type) => flags & type.flag);
  if (active.length === 0) {
    return [HITSOUND_TYPE_ORDER[HITSOUND_TYPE_ORDER.length - 1]];
  }

  const hasNonNormalAddition = active.some((type) => type.flag !== HITSOUND_FLAG_NORMAL);
  if (hasNonNormalAddition) {
    active = active.filter((type) => type.flag !== HITSOUND_FLAG_NORMAL);
  }

  return active;
}

export function getDominantHitsoundColor(flags: number): string {
  return getOrderedActiveHitsoundTypes(flags)[0].color;
}

export function getSecondaryHitsoundColors(flags: number): string[] {
  return getOrderedActiveHitsoundTypes(flags)
    .slice(1)
    .map((type) => type.color);
}

export function getHitsoundCircleLayout(baseRadius: number, flags: number): HitsoundCircleLayout {
  const ringCount = getSecondaryHitsoundColors(flags).length;
  const multiRing = ringCount >= 2;

  const fillRadius = baseRadius - (multiRing ? 1.5 : ringCount === 1 ? 0.5 : 0);
  const ringLineWidth = multiRing ? 2.5 : 2;
  const ringBaseOffset = multiRing ? 1.25 : HITSOUND_RING_BASE_OFFSET;
  const ringStep = multiRing ? 2.5 : HITSOUND_RING_STEP;
  const outerRadius =
    ringCount === 0
      ? baseRadius + 2
      : fillRadius + ringBaseOffset + (ringCount - 1) * ringStep + ringLineWidth;

  return {
    fillRadius,
    ringLineWidth,
    ringBaseOffset,
    ringStep,
    outerRadius,
  };
}

export function getHitsoundCircleOuterRadius(baseRadius: number, flags: number): number {
  return getHitsoundCircleLayout(baseRadius, flags).outerRadius;
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

export function getHitsoundTypesFromFlags(flags: number): HitsoundTypeDisplay[] {
  return getOrderedActiveHitsoundTypes(flags).map((type, index) => ({
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
