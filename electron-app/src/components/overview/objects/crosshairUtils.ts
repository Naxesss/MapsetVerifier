import {
  findNearestSample,
  getPrimaryEdgeSample,
  getSliderWhistleSampleAtTime,
  HITSOUND_FLAG_NORMAL,
  hasSliderBodyWhistle,
  isBaseBodySample,
  isSliderWhistleSample,
  parseHitSoundFlags,
  type HitsoundLayerVisibility,
} from './hitsoundUtils.ts';
import { enrichBodySamplesForDisplay, filterSamplesForHover } from './timelineHitsoundDrawing.ts';
import { findNearestTimelineEdge, getEdgeHitSoundFlags } from './timelineUtils.ts';
import type {
  ObjectsOverviewDifficulty,
  ObjectsTimelineEdge,
  ObjectsTimelineObject,
  ObjectsTimelineSample,
  ObjectsTimingSegment,
} from '../../../Types';

const CROSSHAIR_MATCH_TOLERANCE_MS = 15;

export type CrosshairEdgeEntry = {
  timeMs: number;
  edge: ObjectsTimelineEdge;
  object: ObjectsTimelineObject;
};

export type CrosshairRowLookupCache = {
  sortedEdges: CrosshairEdgeEntry[];
  sortedSamples: ObjectsTimelineSample[];
  enrichedSamples: ObjectsTimelineSample[];
};

export type CrosshairMatchKind =
  | 'edge'
  | 'body-sample'
  | 'tick-sample'
  | 'slider-body'
  | 'spinner-body'
  | 'hold-body'
  | 'none';

export type CrosshairResolvedRow = {
  hasMatch: boolean;
  matchKind: CrosshairMatchKind;
  partName: string;
  hitSoundFlags: number;
  sample: ObjectsTimelineSample | null;
  sampleSource: string | null;
  timelineObject: ObjectsTimelineObject | null;
  slideSample: ObjectsTimelineSample | null;
  whistleSample: ObjectsTimelineSample | null;
  bodyHitSoundFlags: number;
  timingSegment: ObjectsTimingSegment | null;
  nearestPassiveSampleTimeMs: number | null;
};

export function buildCrosshairRowLookupCache(
  difficulty: ObjectsOverviewDifficulty,
  hitsoundLayers: HitsoundLayerVisibility
): CrosshairRowLookupCache {
  const sortedEdges: CrosshairEdgeEntry[] = [];

  for (const object of difficulty.timelineObjects) {
    for (const edge of object.edges) {
      sortedEdges.push({ timeMs: edge.timeMs, edge, object });
    }
  }

  sortedEdges.sort((left, right) => left.timeMs - right.timeMs);

  const enrichedSamples = enrichBodySamplesForDisplay(
    difficulty.timelineSamples ?? [],
    difficulty.timelineObjects
  );
  const sortedSamples = filterSamplesForHover(enrichedSamples, hitsoundLayers)
    .slice()
    .sort((left, right) => left.timeMs - right.timeMs);

  return { sortedEdges, sortedSamples, enrichedSamples };
}

export function findNearestInSortedByTime<T extends { timeMs: number }>(
  items: T[],
  timestampMs: number,
  toleranceMs = CROSSHAIR_MATCH_TOLERANCE_MS
): T | null {
  if (items.length === 0) {
    return null;
  }

  let lo = 0;
  let hi = items.length;

  while (lo < hi) {
    const mid = Math.floor((lo + hi) / 2);
    if (items[mid].timeMs < timestampMs) {
      lo = mid + 1;
    } else {
      hi = mid;
    }
  }

  let best: T | null = null;
  let bestDistance = Number.POSITIVE_INFINITY;

  for (const index of [lo - 1, lo, lo + 1]) {
    if (index < 0 || index >= items.length) {
      continue;
    }

    const distance = Math.abs(items[index].timeMs - timestampMs);
    if (distance > toleranceMs || distance >= bestDistance) {
      continue;
    }

    bestDistance = distance;
    best = items[index];
  }

  return best;
}

function getEdgeMatchTimeMs(
  edgeMatch: CrosshairEdgeEntry | { edge: ObjectsTimelineEdge; object: ObjectsTimelineObject }
): number {
  return 'timeMs' in edgeMatch ? edgeMatch.timeMs : edgeMatch.edge.timeMs;
}

function toEdgeMatch(
  edgeMatch: CrosshairEdgeEntry | { edge: ObjectsTimelineEdge; object: ObjectsTimelineObject }
): CrosshairEdgeEntry {
  return 'timeMs' in edgeMatch
    ? edgeMatch
    : { edge: edgeMatch.edge, object: edgeMatch.object, timeMs: edgeMatch.edge.timeMs };
}

function getTimingSegmentAtTime(
  segments: ObjectsTimingSegment[],
  timestampMs: number
): ObjectsTimingSegment | null {
  for (const segment of segments) {
    if (timestampMs >= segment.startTimeMs && timestampMs < segment.endTimeMs) {
      return segment;
    }
  }

  return null;
}

function findContainingTimelineObject(
  objects: ObjectsTimelineObject[],
  timestampMs: number
): ObjectsTimelineObject | null {
  let best: ObjectsTimelineObject | null = null;
  let bestDuration = Number.POSITIVE_INFINITY;

  for (const object of objects) {
    if (object.objectType === 'Circle') {
      continue;
    }

    if (timestampMs < object.startTimeMs - 1 || timestampMs > object.endTimeMs + 1) {
      continue;
    }

    const duration = object.endTimeMs - object.startTimeMs;
    if (duration < bestDuration) {
      bestDuration = duration;
      best = object;
    }
  }

  return best;
}

function findNearestPassiveSampleInObject(
  samples: ObjectsTimelineSample[],
  object: ObjectsTimelineObject,
  timestampMs: number
): ObjectsTimelineSample | null {
  let best: ObjectsTimelineSample | null = null;
  let bestDistance = Number.POSITIVE_INFINITY;

  for (const sample of samples) {
    if (sample.source !== 'Body' && sample.source !== 'Tick') {
      continue;
    }

    if (sample.timeMs < object.startTimeMs - 1 || sample.timeMs > object.endTimeMs + 1) {
      continue;
    }

    const distance = Math.abs(sample.timeMs - timestampMs);
    if (distance >= bestDistance) {
      continue;
    }

    bestDistance = distance;
    best = sample;
  }

  return best;
}

function findNearestBaseBodySampleInObject(
  samples: ObjectsTimelineSample[],
  object: ObjectsTimelineObject,
  timestampMs: number
): ObjectsTimelineSample | null {
  let best: ObjectsTimelineSample | null = null;
  let bestDistance = Number.POSITIVE_INFINITY;

  for (const sample of samples) {
    if (!isBaseBodySample(sample)) {
      continue;
    }

    if (sample.timeMs < object.startTimeMs - 1 || sample.timeMs > object.endTimeMs + 1) {
      continue;
    }

    const distance = Math.abs(sample.timeMs - timestampMs);
    if (distance >= bestDistance) {
      continue;
    }

    bestDistance = distance;
    best = sample;
  }

  return best;
}

function resolveSliderBodyContext(
  object: ObjectsTimelineObject,
  timestampMs: number,
  samples: ObjectsTimelineSample[],
  timingSegment: ObjectsTimingSegment | null
): CrosshairResolvedRow {
  const nearestPassive = findNearestPassiveSampleInObject(samples, object, timestampMs);
  const slideSample = findNearestBaseBodySampleInObject(samples, object, timestampMs);
  const whistleSample =
    slideSample != null
      ? getSliderWhistleSampleAtTime(samples, slideSample.timeMs, 2)
      : getSliderWhistleSampleAtTime(samples, timestampMs, 15);
  const bodyHitSoundFlags = object.sliderBodyHitSoundFlags ?? 0;

  return {
    hasMatch: true,
    matchKind: 'slider-body',
    partName: 'Slider body',
    hitSoundFlags: bodyHitSoundFlags || HITSOUND_FLAG_NORMAL,
    sample: nearestPassive,
    sampleSource: nearestPassive?.source ?? 'Body',
    timelineObject: object,
    slideSample,
    whistleSample: hasSliderBodyWhistle(bodyHitSoundFlags) ? whistleSample : null,
    bodyHitSoundFlags,
    timingSegment,
    nearestPassiveSampleTimeMs: nearestPassive?.timeMs ?? slideSample?.timeMs ?? null,
  };
}

function resolveLongObjectBodyContext(
  object: ObjectsTimelineObject,
  timestampMs: number,
  samples: ObjectsTimelineSample[],
  timingSegment: ObjectsTimingSegment | null
): CrosshairResolvedRow {
  const nearestPassive = findNearestPassiveSampleInObject(samples, object, timestampMs);
  const matchKind = object.objectType === 'Spinner' ? 'spinner-body' : 'hold-body';

  return {
    hasMatch: true,
    matchKind,
    partName: object.objectType === 'Spinner' ? 'Spinner body' : 'Hold note body',
    hitSoundFlags: HITSOUND_FLAG_NORMAL,
    sample: nearestPassive,
    sampleSource: nearestPassive?.source ?? 'Body',
    timelineObject: object,
    slideSample: nearestPassive?.source === 'Body' ? nearestPassive : null,
    whistleSample: null,
    bodyHitSoundFlags: 0,
    timingSegment,
    nearestPassiveSampleTimeMs: nearestPassive?.timeMs ?? null,
  };
}

function resolvePassiveSampleContext(
  sample: ObjectsTimelineSample,
  samples: ObjectsTimelineSample[],
  timelineObject: ObjectsTimelineObject | null,
  timingSegment: ObjectsTimingSegment | null
): CrosshairResolvedRow {
  const isTick = sample.source === 'Tick';
  let slideSample: ObjectsTimelineSample | null = null;
  let whistleSample: ObjectsTimelineSample | null = null;

  if (sample.source === 'Body') {
    if (isBaseBodySample(sample)) {
      slideSample = sample;
      whistleSample = getSliderWhistleSampleAtTime(samples, sample.timeMs, 2);
    } else if (isSliderWhistleSample(sample)) {
      whistleSample = sample;
      slideSample =
        samples.find(
          (candidate) =>
            isBaseBodySample(candidate) && Math.abs(candidate.timeMs - sample.timeMs) <= 2
        ) ?? null;
    }
  }

  return {
    hasMatch: true,
    matchKind: isTick ? 'tick-sample' : 'body-sample',
    partName: sample.partName ?? sample.source,
    hitSoundFlags: parseHitSoundFlags(sample.hitSound ?? '') || HITSOUND_FLAG_NORMAL,
    sample,
    sampleSource: sample.source,
    timelineObject,
    slideSample,
    whistleSample,
    bodyHitSoundFlags: timelineObject?.sliderBodyHitSoundFlags ?? 0,
    timingSegment,
    nearestPassiveSampleTimeMs: sample.timeMs,
  };
}

export function resolveCrosshairRow(
  difficulty: ObjectsOverviewDifficulty,
  timestampMs: number,
  samples: ObjectsTimelineSample[],
  cache?: CrosshairRowLookupCache
): CrosshairResolvedRow {
  const timingSegment = getTimingSegmentAtTime(difficulty.timingSegments, timestampMs);
  const lookupSamples = cache?.enrichedSamples ?? samples;
  const visibleSamples = cache?.sortedSamples ?? samples;
  const nearestSample = cache
    ? findNearestInSortedByTime(visibleSamples, timestampMs)
    : findNearestSample(visibleSamples, timestampMs);
  const edgeMatch = cache
    ? findNearestInSortedByTime(cache.sortedEdges, timestampMs)
    : findNearestTimelineEdge(difficulty.timelineObjects, timestampMs);

  const nearestDistance = nearestSample
    ? Math.abs(nearestSample.timeMs - timestampMs)
    : Number.POSITIVE_INFINITY;
  const edgeDistance = edgeMatch
    ? Math.abs(getEdgeMatchTimeMs(edgeMatch) - timestampMs)
    : Number.POSITIVE_INFINITY;

  const preferPassiveSample =
    nearestSample != null && nearestSample.source !== 'Edge' && nearestDistance <= edgeDistance;

  if (preferPassiveSample) {
    const containingObject = findContainingTimelineObject(difficulty.timelineObjects, timestampMs);

    return resolvePassiveSampleContext(
      nearestSample,
      lookupSamples,
      containingObject,
      timingSegment
    );
  }

  if (edgeMatch) {
    const edgeEntry = toEdgeMatch(edgeMatch);

    return {
      hasMatch: true,
      matchKind: 'edge',
      partName: edgeEntry.edge.partName,
      hitSoundFlags: getEdgeHitSoundFlags({
        edge: edgeEntry.edge,
        object: edgeEntry.object,
      }),
      sample: getPrimaryEdgeSample(lookupSamples, edgeEntry.timeMs),
      sampleSource: 'Edge',
      timelineObject: edgeEntry.object,
      slideSample: null,
      whistleSample: null,
      bodyHitSoundFlags: 0,
      timingSegment,
      nearestPassiveSampleTimeMs: null,
    };
  }

  if (nearestSample?.source === 'Edge') {
    const edgeAtSample = cache
      ? findNearestInSortedByTime(cache.sortedEdges, nearestSample.timeMs)
      : findNearestTimelineEdge(difficulty.timelineObjects, nearestSample.timeMs);

    return {
      hasMatch: true,
      matchKind: 'edge',
      partName: nearestSample.partName ?? edgeAtSample?.edge.partName ?? 'Edge',
      hitSoundFlags: getEdgeHitSoundFlags(edgeAtSample ? toEdgeMatch(edgeAtSample) : null),
      sample: getPrimaryEdgeSample(lookupSamples, nearestSample.timeMs),
      sampleSource: 'Edge',
      timelineObject: edgeAtSample?.object ?? null,
      slideSample: null,
      whistleSample: null,
      bodyHitSoundFlags: 0,
      timingSegment,
      nearestPassiveSampleTimeMs: null,
    };
  }

  if (nearestSample) {
    const containingObject = findContainingTimelineObject(difficulty.timelineObjects, timestampMs);

    return resolvePassiveSampleContext(
      nearestSample,
      lookupSamples,
      containingObject,
      timingSegment
    );
  }

  const containingObject = findContainingTimelineObject(difficulty.timelineObjects, timestampMs);

  if (containingObject?.objectType === 'Slider') {
    return resolveSliderBodyContext(containingObject, timestampMs, lookupSamples, timingSegment);
  }

  if (
    containingObject &&
    (containingObject.objectType === 'Spinner' || containingObject.objectType === 'Hold note')
  ) {
    return resolveLongObjectBodyContext(
      containingObject,
      timestampMs,
      lookupSamples,
      timingSegment
    );
  }

  return {
    hasMatch: false,
    matchKind: 'none',
    partName: 'Edge',
    hitSoundFlags: HITSOUND_FLAG_NORMAL,
    sample: null,
    sampleSource: null,
    timelineObject: null,
    slideSample: null,
    whistleSample: null,
    bodyHitSoundFlags: 0,
    timingSegment,
    nearestPassiveSampleTimeMs: null,
  };
}
