import {
  MAX_AXIS_PRECISION_ZOOM,
  MAX_TIMELINE_CANVAS_TILE_WIDTH,
  MAX_ZOOM,
  MIN_ZOOM,
  TIMING_SAMPLES_PER_BEAT,
  TIMELINE_INTERVAL_STEPS_MS,
} from './constants.ts';
import {
  getPrimaryEdgeSample,
  buildHitsoundDrawCache,
  type HitsoundDrawCache,
} from './hitsoundUtils.ts';
import { normalizeMode } from '../../../utils/gameMode';
import type {
  ObjectsOverviewDifficulty,
  ObjectsSnappingBucket,
  ObjectsTimelineEdge,
  ObjectsTimelineObject,
  ObjectsTimelineSample,
} from '../../../Types';

/** Lenience matching server-side `HitObject.IsClose` (±2 ms). */
const EDGE_TIME_MATCH_EPSILON_MS = 2;

/** Map rounded edge time (ms) → part label from `ObjectsTimelineEdge.partName`. */
export function buildRoundedEdgePartNameMap(difficulty: ObjectsOverviewDifficulty) {
  const map = new Map<number, string>();
  for (const obj of difficulty.timelineObjects) {
    for (const edge of obj.edges) {
      map.set(Math.round(edge.timeMs), edge.partName);
    }
  }
  return map;
}

export function lookupEdgePartName(
  roundedTimeToPartName: Map<number, string>,
  timeMs: number
): string {
  const rounded = Math.round(timeMs);
  for (let delta = -EDGE_TIME_MATCH_EPSILON_MS; delta <= EDGE_TIME_MATCH_EPSILON_MS; delta += 1) {
    const name = roundedTimeToPartName.get(rounded + delta);
    if (name) {
      return name;
    }
  }
  return 'Unknown';
}

export function getTimelineIntervalMs(durationMs: number, zoom: number) {
  const baseIntervalMs = getAdaptiveBaseIntervalMs(durationMs);
  const normalizedZoom = Math.min(zoom, MAX_AXIS_PRECISION_ZOOM);
  const zoomProgress = Math.max(
    0,
    Math.min(1, (normalizedZoom - MIN_ZOOM) / (MAX_AXIS_PRECISION_ZOOM - MIN_ZOOM))
  );

  if (zoomProgress >= 1) {
    return 1000;
  }

  const scaledIntervalMs = baseIntervalMs * Math.pow(1000 / baseIntervalMs, zoomProgress);
  return getNextTimelineIntervalStep(Math.max(1000, scaledIntervalMs));
}

function getAdaptiveBaseIntervalMs(durationMs: number) {
  const durationSeconds = durationMs / 1000;
  if (durationSeconds <= 30) return 5000;
  if (durationSeconds <= 60) return 10000;
  if (durationSeconds <= 120) return 15000;
  if (durationSeconds <= 300) return 30000;
  if (durationSeconds <= 600) return 60000;
  if (durationSeconds <= 1800) return 120000;
  return 300000;
}

function getNextTimelineIntervalStep(targetIntervalMs: number) {
  return (
    TIMELINE_INTERVAL_STEPS_MS.find((step) => step >= targetIntervalMs) ??
    TIMELINE_INTERVAL_STEPS_MS[TIMELINE_INTERVAL_STEPS_MS.length - 1]
  );
}

export function getTimelineX(
  timeMs: number,
  startTimeMs: number,
  durationMs: number,
  width: number
) {
  return ((timeMs - startTimeMs) / durationMs) * width;
}

export function getTimelineTimeFromX(
  x: number,
  startTimeMs: number,
  durationMs: number,
  width: number
) {
  return startTimeMs + (x / width) * durationMs;
}

export function getAlignedTimelineLineX(
  timeMs: number,
  startTimeMs: number,
  durationMs: number,
  width: number
) {
  return Math.round(getTimelineX(timeMs, startTimeMs, durationMs, width)) + 0.5;
}

/** Next axis tick strictly after `timestampMs` (or the following grid line when already on a tick). */
/** Next axis tick strictly after `timestampMs` (or the following grid line when already on a tick). */
export function getNextTimelineTick(
  timestampMs: number,
  tickIntervalMs: number,
  endTimeMs: number
): number {
  let nextTick = Math.ceil(timestampMs / tickIntervalMs) * tickIntervalMs;

  if (nextTick <= timestampMs) {
    nextTick += tickIntervalMs;
  }

  return Math.min(endTimeMs, nextTick);
}

/** Previous axis tick strictly before `timestampMs` (or the prior grid line when already on a tick). */
export function getPreviousTimelineTick(
  timestampMs: number,
  tickIntervalMs: number,
  startTimeMs: number
): number {
  let previousTick = Math.floor(timestampMs / tickIntervalMs) * tickIntervalMs;

  if (previousTick >= timestampMs) {
    previousTick -= tickIntervalMs;
  }

  return Math.max(startTimeMs, previousTick);
}

export function stepTimelineSeekTimestamp(
  timestampMs: number,
  direction: 1 | -1,
  startTimeMs: number,
  endTimeMs: number,
  tickIntervalMs: number
): number {
  if (direction === 1) {
    return getNextTimelineTick(timestampMs, tickIntervalMs, endTimeMs);
  }

  return getPreviousTimelineTick(timestampMs, tickIntervalMs, startTimeMs);
}

export function getDifficultyKey(difficulty: ObjectsOverviewDifficulty) {
  return `${normalizeMode(difficulty.mode)}::${difficulty.version}`;
}

export function getFirstNoteTimeMs(
  difficulties: ObjectsOverviewDifficulty[],
  visibilityByDifficulty: Record<string, boolean | undefined>,
  getDifficultyKey: (difficulty: ObjectsOverviewDifficulty) => string
): number | null {
  let earliest: number | null = null;

  for (const difficulty of difficulties) {
    if (visibilityByDifficulty[getDifficultyKey(difficulty)] === false) {
      continue;
    }

    for (const timelineObject of difficulty.timelineObjects) {
      if (earliest === null || timelineObject.startTimeMs < earliest) {
        earliest = timelineObject.startTimeMs;
      }
    }
  }

  return earliest;
}

export function getTimestampAtPlayhead(
  scrollLeft: number,
  anchorViewportX: number,
  labelWidth: number,
  timelineWidth: number,
  startTimeMs: number,
  endTimeMs: number
) {
  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const timelineLocalX = scrollLeft + anchorViewportX - labelWidth;
  const clampedX = Math.max(0, Math.min(timelineWidth, timelineLocalX));
  const timestampMs = getTimelineTimeFromX(clampedX, startTimeMs, durationMs, timelineWidth);
  return Math.max(startTimeMs, Math.min(endTimeMs, timestampMs));
}

export function getScrollLeftForTimestamp(
  timestampMs: number,
  anchorViewportX: number,
  labelWidth: number,
  timelineWidth: number,
  startTimeMs: number,
  endTimeMs: number
) {
  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const clampedTimestamp = Math.max(startTimeMs, Math.min(endTimeMs, timestampMs));
  const timelineLocalX = getTimelineX(clampedTimestamp, startTimeMs, durationMs, timelineWidth);
  return timelineLocalX + labelWidth - anchorViewportX;
}

export function findNearestTimelineEdge(
  objects: ObjectsTimelineObject[],
  timestampMs: number,
  toleranceMs = 15
): { edge: ObjectsTimelineEdge; object: ObjectsTimelineObject } | null {
  let best: { edge: ObjectsTimelineEdge; object: ObjectsTimelineObject } | null = null;
  let bestDistance = Number.POSITIVE_INFINITY;

  for (const object of objects) {
    for (const edge of object.edges) {
      const distance = Math.abs(edge.timeMs - timestampMs);
      if (distance > toleranceMs) {
        continue;
      }

      if (distance < bestDistance) {
        bestDistance = distance;
        best = { edge, object };
      }
    }
  }

  return best;
}

export function findEdgeSampleAtTime(
  samples: ObjectsTimelineSample[],
  edgeTimeMs: number,
  toleranceMs = 2
): ObjectsTimelineSample | null {
  return getPrimaryEdgeSample(samples, edgeTimeMs, toleranceMs);
}

export function getEdgeHitSoundFlags(
  match: { edge: ObjectsTimelineEdge; object: ObjectsTimelineObject } | null
): number {
  if (!match) {
    return 0;
  }

  if (match.edge.hitSoundFlags != null && match.edge.hitSoundFlags > 0) {
    return match.edge.hitSoundFlags;
  }

  if (match.object.objectType === 'Circle') {
    return match.object.hitSoundFlags ?? 0;
  }

  return 0;
}

export function areStringArraysEqual(left: string[], right: string[]) {
  if (left.length !== right.length) {
    return false;
  }

  for (let index = 0; index < left.length; index += 1) {
    if (left[index] !== right[index]) {
      return false;
    }
  }

  return true;
}

export function getObjectBodyWidth(
  startX: number,
  endX: number,
  visibleStartX: number,
  visibleEndX: number,
  minimumWidth: number
) {
  const centerX = (startX + endX) / 2;
  let adjustedStartX = startX;
  let adjustedEndX = endX;

  if (adjustedEndX - adjustedStartX < minimumWidth) {
    adjustedStartX = centerX - minimumWidth / 2;
    adjustedEndX = centerX + minimumWidth / 2;
  }

  if (adjustedEndX <= visibleStartX || adjustedStartX >= visibleEndX) {
    return null;
  }

  return {
    startX: Math.max(visibleStartX, adjustedStartX),
    endX: Math.min(visibleEndX, adjustedEndX),
  };
}

export function getTimelineCanvasTiles(width: number) {
  const tiles: Array<{ startX: number; width: number }> = [];

  for (let startX = 0; startX < width; startX += MAX_TIMELINE_CANVAS_TILE_WIDTH) {
    tiles.push({
      startX,
      width: Math.min(MAX_TIMELINE_CANVAS_TILE_WIDTH, width - startX),
    });
  }

  return tiles;
}

export function buildRoundedEdgeTimes(timelineObjects: ObjectsTimelineObject[]): Set<number> {
  const roundedEdgeTimes = new Set<number>();

  for (const object of timelineObjects) {
    for (const edge of object.edges) {
      roundedEdgeTimes.add(Math.round(edge.timeMs));
    }
  }

  return roundedEdgeTimes;
}

export type TimelineRowDrawCache = {
  roundedEdgeTimes: Set<number>;
  hitsound?: HitsoundDrawCache;
};

export function buildTimelineRowDrawCache(
  timelineObjects: ObjectsTimelineObject[],
  samples: ObjectsTimelineSample[] | undefined,
  isHitsoundView: boolean
): TimelineRowDrawCache {
  return {
    roundedEdgeTimes: buildRoundedEdgeTimes(timelineObjects),
    hitsound: isHitsoundView ? buildHitsoundDrawCache(timelineObjects, samples ?? []) : undefined,
  };
}

/** Visible timing-grid snap ticks (same rules as `drawTimingGrid`). */
export function buildTimelineSnapTicks(
  difficulties: ObjectsOverviewDifficulty[],
  startTimeMs: number,
  endTimeMs: number
): number[] {
  const roundedEdgeTimes = new Set<number>();
  const tickTimes = new Set<number>();

  for (const difficulty of difficulties) {
    for (const edgeTimeMs of buildRoundedEdgeTimes(difficulty.timelineObjects)) {
      roundedEdgeTimes.add(edgeTimeMs);
    }

    for (const segment of difficulty.timingSegments) {
      const sampleStepMs = segment.msPerBeat / TIMING_SAMPLES_PER_BEAT;
      if (sampleStepMs <= 0) {
        continue;
      }

      const visibleStartMs = Math.max(startTimeMs, segment.startTimeMs);
      const visibleEndMs = Math.min(endTimeMs, segment.endTimeMs);
      if (visibleEndMs <= visibleStartMs) {
        continue;
      }

      const startSampleIndex = Math.max(
        0,
        Math.ceil((visibleStartMs - segment.offsetMs) / sampleStepMs)
      );
      const endSampleIndex = Math.floor((visibleEndMs - segment.offsetMs) / sampleStepMs);

      for (let sampleIndex = startSampleIndex; sampleIndex <= endSampleIndex; sampleIndex += 1) {
        const timeMs = segment.offsetMs + sampleIndex * sampleStepMs;
        const hasNearbyEdge = hasNearbyRoundedEdge(roundedEdgeTimes, timeMs);
        if (getTimingTickStyle(sampleIndex, segment.meter, hasNearbyEdge)) {
          tickTimes.add(timeMs);
        }
      }
    }
  }

  return Array.from(tickTimes).sort((left, right) => left - right);
}

export function getAdjacentTimingSnapTick(
  sortedTicks: number[],
  timestampMs: number,
  direction: 1 | -1,
  startTimeMs: number,
  endTimeMs: number
): number {
  if (sortedTicks.length === 0) {
    return Math.max(startTimeMs, Math.min(endTimeMs, timestampMs));
  }

  if (direction === 1) {
    for (const tick of sortedTicks) {
      if (tick > timestampMs + 1e-6) {
        return Math.min(endTimeMs, tick);
      }
    }

    return endTimeMs;
  }

  for (let index = sortedTicks.length - 1; index >= 0; index -= 1) {
    if (sortedTicks[index] < timestampMs - 1e-6) {
      return Math.max(startTimeMs, sortedTicks[index]);
    }
  }

  return startTimeMs;
}

/** Beat snap colors aligned with osu! editor / timeline tick grid. */
export const SNAP_DIVISOR_COLORS: Partial<Record<number, string>> = {
  1: 'rgb(255,255,255)',
  2: 'rgb(255,120,100)',
  3: 'rgb(255,100,225)',
  4: 'rgb(120,175,255)',
  6: 'rgb(200,150,255)',
  8: 'rgb(255,225,100)',
  12: 'rgb(125,135,150)',
  16: 'rgb(125,135,150)',
};

export function getSnapLabelColor(snapLabel: string) {
  if (snapLabel === 'Unsnapped') {
    return 'var(--mantine-color-orange-4)';
  }

  if (snapLabel === 'Unknown') {
    return 'var(--mantine-color-dimmed)';
  }

  const match = /^1\/(\d+)$/.exec(snapLabel);
  if (!match) {
    return 'var(--mantine-color-dimmed)';
  }

  const divisor = Number(match[1]);
  return SNAP_DIVISOR_COLORS[divisor] ?? 'rgb(125,135,150)';
}

export function getTimingTickStyle(sampleIndex: number, meter: number, hasNearbyEdge: boolean) {
  const shouldShowTick =
    sampleIndex % (TIMING_SAMPLES_PER_BEAT / 4) === 0 ||
    (hasNearbyEdge &&
      (sampleIndex % (TIMING_SAMPLES_PER_BEAT / 12) === 0 ||
        sampleIndex % (TIMING_SAMPLES_PER_BEAT / 16) === 0));

  if (!shouldShowTick) {
    return null;
  }

  const safeMeter = Math.max(1, meter || 1);
  const baseHeight = hasNearbyEdge ? 6 : 3;

  if (sampleIndex % (TIMING_SAMPLES_PER_BEAT * safeMeter) === 0) {
    return { color: SNAP_DIVISOR_COLORS[1]!, height: 12, alpha: 0.5, priority: 8 };
  }

  if (sampleIndex % TIMING_SAMPLES_PER_BEAT === 0) {
    return { color: SNAP_DIVISOR_COLORS[1]!, height: 6, alpha: 0.5, priority: 7 };
  }

  if (sampleIndex % (TIMING_SAMPLES_PER_BEAT / 2) === 0) {
    return { color: SNAP_DIVISOR_COLORS[2]!, height: baseHeight, alpha: 0.5, priority: 6 };
  }

  if (sampleIndex % (TIMING_SAMPLES_PER_BEAT / 3) === 0) {
    return { color: SNAP_DIVISOR_COLORS[3]!, height: baseHeight, alpha: 0.5, priority: 5 };
  }

  if (sampleIndex % (TIMING_SAMPLES_PER_BEAT / 4) === 0) {
    return { color: SNAP_DIVISOR_COLORS[4]!, height: baseHeight, alpha: 0.5, priority: 4 };
  }

  if (sampleIndex % (TIMING_SAMPLES_PER_BEAT / 6) === 0) {
    return { color: SNAP_DIVISOR_COLORS[6]!, height: baseHeight, alpha: 0.5, priority: 3 };
  }

  if (sampleIndex % (TIMING_SAMPLES_PER_BEAT / 8) === 0) {
    return { color: SNAP_DIVISOR_COLORS[8]!, height: baseHeight, alpha: 0.5, priority: 2 };
  }

  if (
    sampleIndex % (TIMING_SAMPLES_PER_BEAT / 12) === 0 ||
    sampleIndex % (TIMING_SAMPLES_PER_BEAT / 16) === 0
  ) {
    return { color: SNAP_DIVISOR_COLORS[12]!, height: baseHeight, alpha: 0.5, priority: 1 };
  }

  return { color: 'rgb(255,0,0)', height: 12, alpha: 0.5, priority: 0 };
}

export function getZoomStep(zoom: number) {
  if (zoom >= 12) return 2;
  if (zoom >= 6) return 1;
  return 0.5;
}

export function clampZoom(zoom: number) {
  return Math.max(MIN_ZOOM, Math.min(MAX_ZOOM, Math.round(zoom * 100) / 100));
}

export function formatZoom(zoom: number) {
  const rounded = Math.round(zoom * 10) / 10;
  return Number.isInteger(rounded) ? rounded.toFixed(0) : rounded.toFixed(1);
}

export function hasNearbyRoundedEdge(roundedEdgeTimes: Set<number>, timeMs: number) {
  const roundedTimeMs = Math.round(timeMs);

  for (let delta = -2; delta <= 2; delta += 1) {
    if (roundedEdgeTimes.has(roundedTimeMs + delta)) {
      return true;
    }
  }

  return false;
}

export function getSnappingColumns(difficulties: ObjectsOverviewDifficulty[]) {
  const columnMap = new Map<
    string,
    {
      bucket: ObjectsSnappingBucket;
      hasNonZeroCount: boolean;
    }
  >();

  for (const difficulty of difficulties) {
    for (const bucket of difficulty.snappings) {
      const existing = columnMap.get(bucket.label);
      const hasNonZeroCount = (existing?.hasNonZeroCount ?? false) || bucket.count > 0;

      if (!existing || bucket.divisor < existing.bucket.divisor) {
        columnMap.set(bucket.label, {
          bucket,
          hasNonZeroCount,
        });
        continue;
      }

      if (hasNonZeroCount !== existing.hasNonZeroCount) {
        columnMap.set(bucket.label, {
          bucket: existing.bucket,
          hasNonZeroCount,
        });
      }
    }
  }

  return Array.from(columnMap.values())
    .filter((column) => column.hasNonZeroCount)
    .map((column) => column.bucket)
    .sort((left, right) => left.divisor - right.divisor);
}

export function formatDuration(durationMs: number) {
  const safeDuration = Math.max(0, durationMs);
  const totalSeconds = Math.floor(safeDuration / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, '0')}`;
}

export function formatTime(timeMs: number) {
  const absoluteMs = Math.abs(timeMs);
  const totalSeconds = Math.floor(absoluteMs / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  const sign = timeMs < 0 ? '-' : '';
  return `${sign}${minutes}:${seconds.toString().padStart(2, '0')}`;
}

export function formatEditorTimestamp(timeMs: number) {
  const clampedMs = Math.max(0, Math.round(timeMs));
  const totalSeconds = Math.floor(clampedMs / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  const milliseconds = clampedMs % 1000;
  return `${minutes.toString().padStart(2, '0')}:${seconds
    .toString()
    .padStart(2, '0')}:${milliseconds.toString().padStart(3, '0')}`;
}

function isEdgeTimeMatch(leftMs: number, rightMs: number) {
  return Math.abs(leftMs - rightMs) <= EDGE_TIME_MATCH_EPSILON_MS;
}

/** Returns the snap label (e.g. `1/4`) or `Unsnapped` for an edge timestamp. */
export function lookupEdgeSnapLabel(difficulty: ObjectsOverviewDifficulty, timeMs: number): string {
  if (difficulty.unsnappedEdgeTimesMs?.some((candidate) => isEdgeTimeMatch(candidate, timeMs))) {
    return 'Unsnapped';
  }

  for (const bucket of difficulty.snappings) {
    if (bucket.edgeTimesMs?.some((candidate) => isEdgeTimeMatch(candidate, timeMs))) {
      return bucket.label;
    }
  }

  return 'Unknown';
}
