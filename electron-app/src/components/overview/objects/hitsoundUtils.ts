import type { ObjectsTimelineObject, ObjectsTimelineSample } from '../../../Types';

export const HITSOUND_FLAG_NORMAL = 1;
export const HITSOUND_FLAG_WHISTLE = 2;
export const HITSOUND_FLAG_FINISH = 4;
export const HITSOUND_FLAG_CLAP = 8;

/** Sample bank colours (Normal / Soft / Drum). Normal is explicit and rare in modern maps. */
export const EDITOR_SAMPLE_BANK_COLORS = {
  Normal: '#a78bfa',
  Soft: '#fbbf24',
  Drum: '#38bdf8',
  Auto: '#a78bfa',
} as const;

/** Hitsound addition colours. Finish/Clap follow Soft/Drum; Whistle uses green. */
export const HITSOUND_COLORS = {
  normal: EDITOR_SAMPLE_BANK_COLORS.Normal,
  whistle: '#34d399',
  finish: EDITOR_SAMPLE_BANK_COLORS.Soft,
  clap: EDITOR_SAMPLE_BANK_COLORS.Drum,
  body: '#52525b',
  tick: '#71717a',
} as const;

export const SAMPLESET_TINTS: Record<string, string> = {
  Normal: EDITOR_SAMPLE_BANK_COLORS.Normal,
  Soft: EDITOR_SAMPLE_BANK_COLORS.Soft,
  Drum: EDITOR_SAMPLE_BANK_COLORS.Drum,
  Auto: EDITOR_SAMPLE_BANK_COLORS.Auto,
};

export const SAMPLESET_OVERLAY_ALPHA = 0.07;
export const SAMPLESET_ACCENT_ALPHA = 0.45;
export const SAMPLESET_BODY_ALPHA = 0.55;

/** Distinct from Soft section tint (amber) and sample bank colours. */
export const HITSOUND_GAP_OVERLAY_COLOR = '#d946ef';
export const HITSOUND_GAP_OVERLAY_ALPHA = 0.14;

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

export function getSamplesetColor(sampleset: string): string {
  return SAMPLESET_TINTS[sampleset] ?? SAMPLESET_TINTS.Normal;
}

export function findObjectBodySample(
  samples: ObjectsTimelineSample[],
  timelineObject: ObjectsTimelineObject
): ObjectsTimelineSample | null {
  if (timelineObject.objectType === 'Circle') {
    return null;
  }

  const bodySamples = samples.filter((sample) => sample.source === 'Body');
  return findPrimaryBodySampleInRange(
    bodySamples,
    timelineObject.startTimeMs,
    timelineObject.endTimeMs
  );
}

export function isBaseEdgeSample(sample: ObjectsTimelineSample): boolean {
  return !sample.hitSound || sample.hitSound === 'Normal';
}

export function isBaseBodySample(sample: ObjectsTimelineSample): boolean {
  return sample.source === 'Body' && isBaseEdgeSample(sample);
}

export function isBodyAdditionSample(sample: ObjectsTimelineSample): boolean {
  return sample.source === 'Body' && !isBaseEdgeSample(sample);
}

/** Drop redundant sliderslide markers when a body addition sample exists at the same time. */
export function dedupePassiveBodySamples(samples: ObjectsTimelineSample[]): ObjectsTimelineSample[] {
  const additionTimes = new Set<number>();

  for (const sample of samples) {
    if (isBodyAdditionSample(sample)) {
      additionTimes.add(sample.timeMs);
    }
  }

  if (additionTimes.size === 0) {
    return samples;
  }

  return samples.filter(
    (sample) => !(isBaseBodySample(sample) && additionTimes.has(sample.timeMs))
  );
}

export function hasSliderBodyAddition(flags: number): boolean {
  return (
    (flags & (HITSOUND_FLAG_WHISTLE | HITSOUND_FLAG_FINISH | HITSOUND_FLAG_CLAP)) !== 0
  );
}

function findPrimaryBodySampleInRange(
  bodySamples: ObjectsTimelineSample[],
  startTimeMs: number,
  endTimeMs: number
): ObjectsTimelineSample | null {
  let fallback: ObjectsTimelineSample | null = null;

  for (const sample of bodySamples) {
    if (sample.timeMs < startTimeMs - 1 || sample.timeMs > endTimeMs + 1) {
      continue;
    }

    if (isBaseBodySample(sample)) {
      return sample;
    }

    fallback ??= sample;
  }

  return fallback;
}

/** Prefer the base hitnormal sample; osu! emits separate samples per addition at the same edge. */
export function getPrimaryEdgeSample(
  samples: ObjectsTimelineSample[],
  edgeTimeMs: number,
  toleranceMs = 2
): ObjectsTimelineSample | null {
  let bestBase: ObjectsTimelineSample | null = null;
  let bestBaseDistance = Number.POSITIVE_INFINITY;
  let bestAny: ObjectsTimelineSample | null = null;
  let bestAnyDistance = Number.POSITIVE_INFINITY;

  for (const sample of samples) {
    if (sample.source !== 'Edge') {
      continue;
    }

    const distance = Math.abs(sample.timeMs - edgeTimeMs);
    if (distance > toleranceMs) {
      continue;
    }

    if (distance < bestAnyDistance) {
      bestAnyDistance = distance;
      bestAny = sample;
    }

    if (isBaseEdgeSample(sample) && distance < bestBaseDistance) {
      bestBaseDistance = distance;
      bestBase = sample;
    }
  }

  return bestBase ?? bestAny;
}

export type PrimaryEdgeMarker = {
  timeMs: number;
  sampleset: string;
};

/** One strip bar per object edge, using the same hitnormal resolution as the crosshair panel. */
export function getPrimaryEdgeMarkers(
  timelineObjects: ObjectsTimelineObject[],
  samples: ObjectsTimelineSample[]
): PrimaryEdgeMarker[] {
  const markers: PrimaryEdgeMarker[] = [];

  for (const object of timelineObjects) {
    for (const edge of object.edges) {
      const primary = getPrimaryEdgeSample(samples, edge.timeMs);
      if (!primary) {
        continue;
      }

      markers.push({
        timeMs: edge.timeMs,
        sampleset: primary.sampleset,
      });
    }
  }

  return markers;
}

export type HitsoundDrawCache = {
  primaryEdgeMarkers: PrimaryEdgeMarker[];
  bodySampleByObject: Map<ObjectsTimelineObject, ObjectsTimelineSample | null>;
};

export function buildHitsoundDrawCache(
  timelineObjects: ObjectsTimelineObject[],
  samples: ObjectsTimelineSample[]
): HitsoundDrawCache {
  const bodySamples: ObjectsTimelineSample[] = [];
  for (const sample of samples) {
    if (sample.source === 'Body') {
      bodySamples.push(sample);
    }
  }

  const bodySampleByObject = new Map<ObjectsTimelineObject, ObjectsTimelineSample | null>();
  for (const object of timelineObjects) {
    if (object.objectType === 'Circle') {
      continue;
    }

    bodySampleByObject.set(
      object,
      findPrimaryBodySampleInRange(bodySamples, object.startTimeMs, object.endTimeMs)
    );
  }

  return {
    primaryEdgeMarkers: getPrimaryEdgeMarkers(timelineObjects, samples),
    bodySampleByObject,
  };
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
