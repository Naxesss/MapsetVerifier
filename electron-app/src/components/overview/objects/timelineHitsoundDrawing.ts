import {
  getDominantHitsoundColor,
  getSamplesetColor,
  hasSliderBodyWhistle,
  isBaseBodySample,
  isBaseEdgeSample,
  isSliderWhistleSample,
  parseHitSoundFlags,
  HITSOUND_GAP_OVERLAY_ALPHA,
  HITSOUND_GAP_OVERLAY_COLOR,
  SAMPLESET_ACCENT_ALPHA,
  SAMPLESET_OVERLAY_ALPHA,
  type HitsoundLayerVisibility,
  type PrimaryEdgeMarker,
} from './hitsoundUtils.ts';
import {
  findLowerBoundIndex,
  getObjectBodyWidth,
  getTimelineTimeFromX,
  getTimelineX,
  type TimelineSoundStripCache,
} from './timelineUtils.ts';
import { withAlpha } from '../../../utils/color.ts';
import type {
  ObjectsHitsoundGapPeriod,
  ObjectsOverviewDifficulty,
  ObjectsTimelineObject,
  ObjectsTimelineSample,
  ObjectsTimingSegment,
} from '../../../Types';

export const SOUND_STRIP_TOP_OFFSET = 6;
export const SOUND_STRIP_EDGE_LANE_HEIGHT = 16;
export const SOUND_STRIP_PASSIVE_LANE_HEIGHT = 11;
export const SOUND_STRIP_LANE_GAP = 2;
export const EDGE_MARKER_WIDTH = 4;
export const BODY_MARKER_WIDTH = 8;
export const BODY_MARKER_HEIGHT = 5;
export const BODY_MARKER_PAIR_OFFSET = 5;
export const TICK_MARKER_RADIUS = 3;

export type SoundStripBounds = {
  top: number;
  bottom: number;
  height: number;
  edgeLaneTop: number;
  edgeLaneHeight: number;
  passiveLaneTop: number;
  passiveLaneHeight: number;
};

export function getSoundStripBounds(height: number): SoundStripBounds {
  const top = height / 2 + SOUND_STRIP_TOP_OFFSET;
  const edgeLaneTop = top;
  const edgeLaneHeight = SOUND_STRIP_EDGE_LANE_HEIGHT;
  const passiveLaneTop = edgeLaneTop + edgeLaneHeight + SOUND_STRIP_LANE_GAP;
  const passiveLaneHeight = SOUND_STRIP_PASSIVE_LANE_HEIGHT;
  const stripHeight = edgeLaneHeight + SOUND_STRIP_LANE_GAP + passiveLaneHeight;

  return {
    top,
    bottom: top + stripHeight,
    height: stripHeight,
    edgeLaneTop,
    edgeLaneHeight,
    passiveLaneTop,
    passiveLaneHeight,
  };
}

export function drawHitsoundGapOverlay(
  ctx: CanvasRenderingContext2D,
  {
    gapPeriods,
    startTimeMs,
    durationMs,
    width,
    visibleStartX,
    visibleEndX,
    height,
  }: {
    gapPeriods: ObjectsHitsoundGapPeriod[];
    startTimeMs: number;
    durationMs: number;
    width: number;
    visibleStartX: number;
    visibleEndX: number;
    height: number;
  }
) {
  for (const gap of gapPeriods) {
    if (gap.endTimeMs <= gap.startTimeMs) continue;

    const startX = getTimelineX(gap.startTimeMs, startTimeMs, durationMs, width);
    const endX = getTimelineX(gap.endTimeMs, startTimeMs, durationMs, width);
    const bounds = getObjectBodyWidth(startX, endX, visibleStartX, visibleEndX, 4);
    if (!bounds) continue;

    ctx.save();
    ctx.fillStyle = withAlpha(HITSOUND_GAP_OVERLAY_COLOR, HITSOUND_GAP_OVERLAY_ALPHA);
    ctx.fillRect(bounds.startX, 2, bounds.endX - bounds.startX, height - 4);
    ctx.restore();
  }
}

function drawLaneDivider(
  ctx: CanvasRenderingContext2D,
  bounds: SoundStripBounds,
  visibleStartX: number,
  visibleEndX: number
) {
  const dividerY = bounds.passiveLaneTop - SOUND_STRIP_LANE_GAP / 2;
  ctx.strokeStyle = withAlpha('#ffffff', 0.14);
  ctx.lineWidth = 1;
  ctx.beginPath();
  ctx.moveTo(visibleStartX, dividerY);
  ctx.lineTo(visibleEndX, dividerY);
  ctx.stroke();
}

export function drawSamplesetRegions(
  ctx: CanvasRenderingContext2D,
  {
    timingSegments,
    startTimeMs,
    endTimeMs,
    durationMs,
    width,
    visibleStartX,
    visibleEndX,
    height,
  }: {
    timingSegments: ObjectsTimingSegment[];
    startTimeMs: number;
    endTimeMs: number;
    durationMs: number;
    width: number;
    visibleStartX: number;
    visibleEndX: number;
    height: number;
  }
) {
  for (const segment of timingSegments) {
    if (!segment.sampleset) continue;

    const sampleset = segment.sampleset;
    if (!sampleset || sampleset === 'Auto') continue;

    const segStart = Math.max(startTimeMs, segment.startTimeMs);
    const segEnd = Math.min(endTimeMs, segment.endTimeMs);
    if (segEnd <= segStart) continue;

    const startX = getTimelineX(segStart, startTimeMs, durationMs, width);
    const endX = getTimelineX(segEnd, startTimeMs, durationMs, width);
    const bounds = getObjectBodyWidth(startX, endX, visibleStartX, visibleEndX, 2);
    if (!bounds) continue;

    const tint = getSamplesetColor(sampleset);
    const regionWidth = bounds.endX - bounds.startX;

    ctx.fillStyle = withAlpha(tint, SAMPLESET_OVERLAY_ALPHA);
    ctx.fillRect(bounds.startX, 0, regionWidth, height);

    ctx.fillStyle = withAlpha(tint, SAMPLESET_ACCENT_ALPHA);
    ctx.fillRect(bounds.startX, 0, regionWidth, 2);
  }
}

function drawEdgeMarker(
  ctx: CanvasRenderingContext2D,
  x: number,
  bounds: SoundStripBounds,
  color: string
) {
  const markerHeight = bounds.edgeLaneHeight - 2;
  const markerTop = bounds.edgeLaneTop + 1;
  const markerLeft = x - EDGE_MARKER_WIDTH / 2;

  ctx.fillStyle = color;
  ctx.fillRect(markerLeft, markerTop, EDGE_MARKER_WIDTH, markerHeight);

  ctx.strokeStyle = withAlpha('#ffffff', 0.45);
  ctx.lineWidth = 1;
  ctx.strokeRect(markerLeft + 0.5, markerTop + 0.5, EDGE_MARKER_WIDTH - 1, markerHeight - 1);
}

function drawBodyMarker(
  ctx: CanvasRenderingContext2D,
  x: number,
  bounds: SoundStripBounds,
  color: string
) {
  const markerLeft = x - BODY_MARKER_WIDTH / 2;
  const markerTop = bounds.passiveLaneTop + (bounds.passiveLaneHeight - BODY_MARKER_HEIGHT) / 2;

  ctx.fillStyle = color;
  ctx.fillRect(markerLeft, markerTop, BODY_MARKER_WIDTH, BODY_MARKER_HEIGHT);

  ctx.strokeStyle = withAlpha('#ffffff', 0.3);
  ctx.lineWidth = 1;
  ctx.strokeRect(markerLeft + 0.5, markerTop + 0.5, BODY_MARKER_WIDTH - 1, BODY_MARKER_HEIGHT - 1);
}

function drawTickMarker(
  ctx: CanvasRenderingContext2D,
  x: number,
  bounds: SoundStripBounds,
  color: string
) {
  const centerY = bounds.passiveLaneTop + bounds.passiveLaneHeight / 2;

  ctx.fillStyle = color;
  ctx.beginPath();
  ctx.arc(x, centerY, TICK_MARKER_RADIUS, 0, Math.PI * 2);
  ctx.fill();

  ctx.strokeStyle = withAlpha('#ffffff', 0.25);
  ctx.lineWidth = 1;
  ctx.stroke();
}

const PASSIVE_MARKER_CULL_PADDING = 2;

function getPassiveMarkerCullMargin(): number {
  return (
    Math.max(BODY_MARKER_WIDTH + BODY_MARKER_PAIR_OFFSET, TICK_MARKER_RADIUS * 2) / 2 +
    PASSIVE_MARKER_CULL_PADDING
  );
}

function buildPairedBodyTimes(samples: ObjectsTimelineSample[]): Set<number> {
  const slideTimes = new Set<number>();
  const whistleTimes = new Set<number>();

  for (const sample of samples) {
    if (sample.source !== 'Body') {
      continue;
    }

    if (isBaseBodySample(sample)) {
      slideTimes.add(sample.timeMs);
    } else if (isSliderWhistleSample(sample)) {
      whistleTimes.add(sample.timeMs);
    }
  }

  const paired = new Set<number>();
  for (const timeMs of slideTimes) {
    if (whistleTimes.has(timeMs)) {
      paired.add(timeMs);
    }
  }

  return paired;
}

function getBodyMarkerX(
  sample: ObjectsTimelineSample,
  x: number,
  pairedBodyTimes: Set<number>
): number {
  if (!pairedBodyTimes.has(sample.timeMs)) {
    return x;
  }

  if (isSliderWhistleSample(sample)) {
    return x + BODY_MARKER_PAIR_OFFSET;
  }

  if (isBaseBodySample(sample)) {
    return x - BODY_MARKER_PAIR_OFFSET;
  }

  return x;
}

function isNonDecreasingTime(samples: ObjectsTimelineSample[]): boolean {
  for (let index = 1; index < samples.length; index += 1) {
    if (samples[index].timeMs < samples[index - 1].timeMs) {
      return false;
    }
  }

  return true;
}

function sliceTimeRange(
  samples: ObjectsTimelineSample[],
  rangeStart: number,
  rangeEnd: number
): ObjectsTimelineSample[] {
  const start = findLowerBoundIndex(samples, rangeStart, (sample) => sample.timeMs);
  let end = findLowerBoundIndex(samples, rangeEnd, (sample) => sample.timeMs);
  while (end < samples.length && samples[end].timeMs <= rangeEnd) {
    end += 1;
  }

  return samples.slice(start, end);
}

function findFirstBaseEdgeNear(
  baseEdgesByTime: { sample: ObjectsTimelineSample; index: number }[],
  timeMs: number
): ObjectsTimelineSample | undefined {
  const start = findLowerBoundIndex(baseEdgesByTime, timeMs - 2, (entry) => entry.sample.timeMs);
  let end = findLowerBoundIndex(baseEdgesByTime, timeMs + 2, (entry) => entry.sample.timeMs);
  while (end < baseEdgesByTime.length && baseEdgesByTime[end].sample.timeMs <= timeMs + 2) {
    end += 1;
  }

  let bestIndex = Number.POSITIVE_INFINITY;
  let best: ObjectsTimelineSample | undefined;

  for (let index = start; index < end; index += 1) {
    const entry = baseEdgesByTime[index];
    if (Math.abs(entry.sample.timeMs - timeMs) > 2 || entry.index >= bestIndex) {
      continue;
    }

    best = entry.sample;
    bestIndex = entry.index;
  }

  return best;
}

function orderIndicesByTime(items: { timeMs: number }[]): number[] {
  const order = items.map((_, index) => index);
  order.sort((left, right) => items[left].timeMs - items[right].timeMs || left - right);
  return order;
}

export function enrichBodySamplesForDisplay(
  samples: ObjectsTimelineSample[],
  timelineObjects: ObjectsTimelineObject[]
): ObjectsTimelineSample[] {
  const enriched = [...samples];
  const whistleTimes = new Set<number>();
  const whistleSamples: ObjectsTimelineSample[] = [];
  const baseBodySamples: ObjectsTimelineSample[] = [];
  const baseEdges: { sample: ObjectsTimelineSample; index: number }[] = [];

  for (let index = 0; index < samples.length; index += 1) {
    const sample = samples[index];
    if (isSliderWhistleSample(sample)) {
      whistleTimes.add(sample.timeMs);
      whistleSamples.push(sample);
    }

    if (sample.source === 'Body' && isBaseBodySample(sample)) {
      baseBodySamples.push(sample);
    }

    if (sample.source === 'Edge' && isBaseEdgeSample(sample)) {
      baseEdges.push({ sample, index });
    }
  }

  const whistleSamplesByTime = whistleSamples
    .slice()
    .sort((left, right) => left.timeMs - right.timeMs);
  const baseEdgesByTime = baseEdges
    .slice()
    .sort((left, right) => left.sample.timeMs - right.sample.timeMs || left.index - right.index);
  const baseBodyIsTimeSorted = isNonDecreasingTime(baseBodySamples);

  for (const object of timelineObjects) {
    if (object.objectType !== 'Slider') {
      continue;
    }

    const rangeStart = object.startTimeMs - 1;
    const rangeEnd = object.endTimeMs + 1;
    const inRangeBaseSamples = baseBodyIsTimeSorted
      ? sliceTimeRange(baseBodySamples, rangeStart, rangeEnd)
      : baseBodySamples.filter(
          (sample) => sample.timeMs >= rangeStart && sample.timeMs <= rangeEnd
        );
    const whistleStart = findLowerBoundIndex(
      whistleSamplesByTime,
      rangeStart,
      (sample) => sample.timeMs
    );
    const hasWhistleInRange =
      whistleStart < whistleSamplesByTime.length &&
      whistleSamplesByTime[whistleStart].timeMs <= rangeEnd;

    if (
      inRangeBaseSamples.length === 0 &&
      hasSliderBodyWhistle(object.sliderBodyHitSoundFlags ?? 0)
    ) {
      const headSample = findFirstBaseEdgeNear(baseEdgesByTime, object.startTimeMs);
      const fallbackSampleset = headSample?.sampleset ?? 'Normal';

      enriched.push({
        timeMs: object.startTimeMs,
        source: 'Body',
        hitSound: null,
        sampleset: fallbackSampleset,
        customIndex: headSample?.customIndex ?? 1,
        partName: null,
        objectType: 'Slider',
      });
      inRangeBaseSamples.push(enriched[enriched.length - 1]);
    }

    if (!hasSliderBodyWhistle(object.sliderBodyHitSoundFlags ?? 0) || hasWhistleInRange) {
      continue;
    }

    for (const sample of inRangeBaseSamples) {
      if (whistleTimes.has(sample.timeMs)) {
        continue;
      }

      enriched.push({
        ...sample,
        hitSound: 'Whistle',
      });
      whistleTimes.add(sample.timeMs);
    }
  }

  return enriched;
}

export function buildSoundStripDrawCache(
  samples: ObjectsTimelineSample[],
  timelineObjects: ObjectsTimelineObject[],
  primaryEdgeMarkers: PrimaryEdgeMarker[]
): TimelineSoundStripCache {
  const passiveSamples = enrichBodySamplesForDisplay(samples, timelineObjects);
  return {
    passiveSamples,
    pairedBodyTimes: buildPairedBodyTimes(passiveSamples),
    passiveOrder: orderIndicesByTime(passiveSamples),
    edgeOrder: orderIndicesByTime(primaryEdgeMarkers),
  };
}

function forEachInTimeWindow<T extends { timeMs: number }>(
  items: T[],
  order: number[],
  startMs: number,
  endMs: number,
  visit: (item: T) => void
) {
  const start = findLowerBoundIndex(order, startMs, (index) => items[index].timeMs);
  let end = findLowerBoundIndex(order, endMs, (index) => items[index].timeMs);
  while (end < order.length && items[order[end]].timeMs <= endMs) {
    end += 1;
  }

  const visible = order.slice(start, end);
  visible.sort((left, right) => left - right);
  for (const index of visible) {
    visit(items[index]);
  }
}

export function drawSoundStrip(
  ctx: CanvasRenderingContext2D,
  {
    difficulty,
    layers,
    startTimeMs,
    durationMs,
    width,
    visibleStartX,
    visibleEndX,
    height,
    primaryEdgeMarkers,
    soundStrip,
  }: {
    difficulty: ObjectsOverviewDifficulty;
    layers: HitsoundLayerVisibility;
    startTimeMs: number;
    durationMs: number;
    width: number;
    visibleStartX: number;
    visibleEndX: number;
    height: number;
    primaryEdgeMarkers: PrimaryEdgeMarker[];
    soundStrip?: TimelineSoundStripCache;
  }
) {
  const samples =
    soundStrip?.passiveSamples ??
    enrichBodySamplesForDisplay(difficulty.timelineSamples ?? [], difficulty.timelineObjects);
  const pairedBodyTimes = soundStrip?.pairedBodyTimes ?? buildPairedBodyTimes(samples);
  const bounds = getSoundStripBounds(height);
  const cullMargin = getPassiveMarkerCullMargin();

  drawLaneDivider(ctx, bounds, visibleStartX, visibleEndX);

  const drawPassiveSample = (sample: ObjectsTimelineSample) => {
    if (sample.source === 'Body' && !layers.body) {
      return;
    }
    if (sample.source === 'Tick' && !layers.ticks) {
      return;
    }
    if (sample.source !== 'Body' && sample.source !== 'Tick') {
      return;
    }

    const x = getTimelineX(sample.timeMs, startTimeMs, durationMs, width);
    if (x < visibleStartX - cullMargin || x > visibleEndX + cullMargin) return;

    if (sample.source === 'Tick') {
      const color = withAlpha(getSamplesetColor(sample.sampleset), 0.9);
      drawTickMarker(ctx, x, bounds, color);
    } else {
      const bodyColor = withAlpha(
        getDominantHitsoundColor(parseHitSoundFlags(sample.hitSound)),
        0.9
      );
      drawBodyMarker(ctx, getBodyMarkerX(sample, x, pairedBodyTimes), bounds, bodyColor);
    }
  };

  if (soundStrip) {
    const windowStartMs =
      getTimelineTimeFromX(visibleStartX - cullMargin, startTimeMs, durationMs, width) - 1;
    const windowEndMs =
      getTimelineTimeFromX(visibleEndX + cullMargin, startTimeMs, durationMs, width) + 1;
    forEachInTimeWindow(
      samples,
      soundStrip.passiveOrder,
      windowStartMs,
      windowEndMs,
      drawPassiveSample
    );
  } else {
    for (const sample of samples) {
      drawPassiveSample(sample);
    }
  }

  const drawEdge = (marker: PrimaryEdgeMarker) => {
    const x = getTimelineX(marker.timeMs, startTimeMs, durationMs, width);
    if (x < visibleStartX - 4 || x > visibleEndX + 4) return;

    drawEdgeMarker(ctx, x, bounds, getSamplesetColor(marker.sampleset));
  };

  if (soundStrip) {
    const windowStartMs =
      getTimelineTimeFromX(visibleStartX - 4, startTimeMs, durationMs, width) - 1;
    const windowEndMs = getTimelineTimeFromX(visibleEndX + 4, startTimeMs, durationMs, width) + 1;
    forEachInTimeWindow(
      primaryEdgeMarkers,
      soundStrip.edgeOrder,
      windowStartMs,
      windowEndMs,
      drawEdge
    );
  } else {
    for (const marker of primaryEdgeMarkers) {
      drawEdge(marker);
    }
  }
}

export function filterSamplesForHover(
  samples: ObjectsTimelineSample[],
  layers: HitsoundLayerVisibility
): ObjectsTimelineSample[] {
  return samples.filter((sample) => {
    if (sample.source === 'Edge') return true;
    if (sample.source === 'Body') return layers.body;
    if (sample.source === 'Tick') return layers.ticks;
    return false;
  });
}
