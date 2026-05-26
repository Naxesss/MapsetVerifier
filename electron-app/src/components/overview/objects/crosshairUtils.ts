import {
  findNearestSample,
  getPrimaryEdgeSample,
  HITSOUND_FLAG_NORMAL,
  parseHitSoundFlags,
  type HitsoundLayerVisibility,
} from './hitsoundUtils.ts';
import { filterSamplesForHover } from './timelineHitsoundDrawing.ts';
import {
  findNearestTimelineEdge,
  getEdgeHitSoundFlags,
} from './timelineUtils.ts';
import type {
  ObjectsOverviewDifficulty,
  ObjectsTimelineEdge,
  ObjectsTimelineObject,
  ObjectsTimelineSample,
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

  const sortedSamples = filterSamplesForHover(
    difficulty.timelineSamples ?? [],
    hitsoundLayers
  )
    .slice()
    .sort((left, right) => left.timeMs - right.timeMs);

  return { sortedEdges, sortedSamples };
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

export function resolveCrosshairRow(
  difficulty: ObjectsOverviewDifficulty,
  timestampMs: number,
  samples: ObjectsTimelineSample[],
  cache?: CrosshairRowLookupCache
) {
  const nearestSample = cache
    ? findNearestInSortedByTime(cache.sortedSamples, timestampMs)
    : findNearestSample(samples, timestampMs);
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
    nearestSample != null &&
    nearestSample.source !== 'Edge' &&
    nearestDistance <= edgeDistance;

  if (preferPassiveSample) {
    return {
      partName: nearestSample.partName ?? nearestSample.source,
      hitSoundFlags: parseHitSoundFlags(nearestSample.hitSound ?? '') || HITSOUND_FLAG_NORMAL,
      sample: nearestSample,
      sampleSource: nearestSample.source,
      hasMatch: true,
    };
  }

  if (edgeMatch) {
    const edgeEntry = toEdgeMatch(edgeMatch);

    return {
      partName: edgeEntry.edge.partName,
      hitSoundFlags: getEdgeHitSoundFlags({
        edge: edgeEntry.edge,
        object: edgeEntry.object,
      }),
      sample: getPrimaryEdgeSample(samples, edgeEntry.timeMs),
      sampleSource: 'Edge' as const,
      hasMatch: true,
    };
  }

  if (nearestSample?.source === 'Edge') {
    const edgeAtSample = cache
      ? findNearestInSortedByTime(cache.sortedEdges, nearestSample.timeMs)
      : findNearestTimelineEdge(difficulty.timelineObjects, nearestSample.timeMs);

    return {
      partName: nearestSample.partName ?? edgeAtSample?.edge.partName ?? 'Edge',
      hitSoundFlags: getEdgeHitSoundFlags(
        edgeAtSample ? toEdgeMatch(edgeAtSample) : null
      ),
      sample: getPrimaryEdgeSample(samples, nearestSample.timeMs),
      sampleSource: 'Edge' as const,
      hasMatch: true,
    };
  }

  if (nearestSample) {
    return {
      partName: nearestSample.partName ?? nearestSample.source,
      hitSoundFlags: parseHitSoundFlags(nearestSample.hitSound ?? '') || HITSOUND_FLAG_NORMAL,
      sample: nearestSample,
      sampleSource: nearestSample.source,
      hasMatch: true,
    };
  }

  return {
    partName: 'Edge',
    hitSoundFlags: HITSOUND_FLAG_NORMAL,
    sample: null,
    sampleSource: null,
    hasMatch: false,
  };
}
