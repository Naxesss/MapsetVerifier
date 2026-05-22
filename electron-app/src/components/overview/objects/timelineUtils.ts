import {
  MAX_AXIS_PRECISION_ZOOM,
  MAX_TIMELINE_CANVAS_TILE_WIDTH,
  MAX_ZOOM,
  MIN_ZOOM,
  TIMING_SAMPLES_PER_BEAT,
  TIMELINE_INTERVAL_STEPS_MS,
} from './constants.ts';
import { normalizeMode } from '../../../utils/gameMode';
import type { ObjectsOverviewDifficulty, ObjectsSnappingBucket, ObjectsTimelineEdge, ObjectsTimelineObject, ObjectsTimelineSample } from '../../../Types';

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

export function getPlayheadViewportX(
  anchorTimeMs: number,
  startTimeMs: number,
  endTimeMs: number,
  timelineWidth: number,
  labelWidth: number
) {
  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  return labelWidth + getTimelineX(anchorTimeMs, startTimeMs, durationMs, timelineWidth);
}

export function getTimestampAtPlayhead(
  scrollLeft: number,
  playheadViewportX: number,
  labelWidth: number,
  timelineWidth: number,
  startTimeMs: number,
  endTimeMs: number
) {
  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const timelineLocalX = scrollLeft + playheadViewportX - labelWidth;
  const clampedX = Math.max(0, Math.min(timelineWidth, timelineLocalX));
  const timestampMs = getTimelineTimeFromX(clampedX, startTimeMs, durationMs, timelineWidth);
  return Math.max(startTimeMs, Math.min(endTimeMs, timestampMs));
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
  let best: ObjectsTimelineSample | null = null;
  let bestDistance = Number.POSITIVE_INFINITY;

  for (const sample of samples) {
    if (sample.source !== 'Edge') {
      continue;
    }

    const distance = Math.abs(sample.timeMs - edgeTimeMs);
    if (distance > toleranceMs) {
      continue;
    }

    if (distance < bestDistance) {
      bestDistance = distance;
      best = sample;
    }
  }

  return best;
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
    return { color: 'rgb(255,255,255)', height: 12, alpha: 0.5, priority: 8 };
  }

  if (sampleIndex % TIMING_SAMPLES_PER_BEAT === 0) {
    return { color: 'rgb(255,255,255)', height: 6, alpha: 0.5, priority: 7 };
  }

  if (sampleIndex % (TIMING_SAMPLES_PER_BEAT / 2) === 0) {
    return { color: 'rgb(255,120,100)', height: baseHeight, alpha: 0.5, priority: 6 };
  }

  if (sampleIndex % (TIMING_SAMPLES_PER_BEAT / 3) === 0) {
    return { color: 'rgb(255,100,225)', height: baseHeight, alpha: 0.5, priority: 5 };
  }

  if (sampleIndex % (TIMING_SAMPLES_PER_BEAT / 4) === 0) {
    return { color: 'rgb(120,175,255)', height: baseHeight, alpha: 0.5, priority: 4 };
  }

  if (sampleIndex % (TIMING_SAMPLES_PER_BEAT / 6) === 0) {
    return { color: 'rgb(200,150,255)', height: baseHeight, alpha: 0.5, priority: 3 };
  }

  if (sampleIndex % (TIMING_SAMPLES_PER_BEAT / 8) === 0) {
    return { color: 'rgb(255,225,100)', height: baseHeight, alpha: 0.5, priority: 2 };
  }

  if (
    sampleIndex % (TIMING_SAMPLES_PER_BEAT / 12) === 0 ||
    sampleIndex % (TIMING_SAMPLES_PER_BEAT / 16) === 0
  ) {
    return { color: 'rgb(125,135,150)', height: baseHeight, alpha: 0.5, priority: 1 };
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
  return Number.isInteger(zoom)
    ? zoom.toFixed(0)
    : zoom.toFixed(2).replace(/0+$/, '').replace(/\.$/, '');
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

