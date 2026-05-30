import {
  buildBaseBodySampleByTime,
  dedupePassiveBodySamples,
  getBodyMarkerFillSample,
  getDominantHitsoundColor,
  getSamplesetColor,
  isBodyAdditionSample,
  parseHitSoundFlags,
  HITSOUND_GAP_OVERLAY_ALPHA,
  HITSOUND_GAP_OVERLAY_COLOR,
  SAMPLESET_ACCENT_ALPHA,
  SAMPLESET_OVERLAY_ALPHA,
  type HitsoundLayerVisibility,
  type PrimaryEdgeMarker,
} from './hitsoundUtils.ts';
import { getTimelineX, getObjectBodyWidth } from './timelineUtils.ts';
import { withAlpha } from '../../../utils/color.ts';
import type {
  ObjectsHitsoundGapPeriod,
  ObjectsOverviewDifficulty,
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
  color: string,
  additionColor?: string
) {
  const markerLeft = x - BODY_MARKER_WIDTH / 2;
  const markerTop =
    bounds.passiveLaneTop + (bounds.passiveLaneHeight - BODY_MARKER_HEIGHT) / 2;

  ctx.fillStyle = color;
  ctx.fillRect(markerLeft, markerTop, BODY_MARKER_WIDTH, BODY_MARKER_HEIGHT);

  if (additionColor) {
    ctx.strokeStyle = withAlpha(additionColor, 0.95);
    ctx.lineWidth = 1.5;
    ctx.strokeRect(
      markerLeft + 0.75,
      markerTop + 0.75,
      BODY_MARKER_WIDTH - 1.5,
      BODY_MARKER_HEIGHT - 1.5
    );
  }
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
  return Math.max(BODY_MARKER_WIDTH, TICK_MARKER_RADIUS * 2) / 2 + PASSIVE_MARKER_CULL_PADDING;
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
  }
) {
  const allSamples = difficulty.timelineSamples ?? [];
  const samples = dedupePassiveBodySamples(allSamples);
  const baseBodyByTime = buildBaseBodySampleByTime(allSamples);
  const bounds = getSoundStripBounds(height);

  drawLaneDivider(ctx, bounds, visibleStartX, visibleEndX);

  for (const sample of samples) {
    if (sample.source === 'Body' && !layers.body) {
      continue;
    }
    if (sample.source === 'Tick' && !layers.ticks) {
      continue;
    }
    if (sample.source !== 'Body' && sample.source !== 'Tick') {
      continue;
    }

    const x = getTimelineX(sample.timeMs, startTimeMs, durationMs, width);
    const cullMargin = getPassiveMarkerCullMargin();
    if (x < visibleStartX - cullMargin || x > visibleEndX + cullMargin) continue;

    const fillSample = getBodyMarkerFillSample(sample, baseBodyByTime);
    const color = withAlpha(getSamplesetColor(fillSample.sampleset), 0.9);
    if (sample.source === 'Tick') {
      drawTickMarker(ctx, x, bounds, color);
    } else {
      const additionColor = isBodyAdditionSample(sample)
        ? getDominantHitsoundColor(parseHitSoundFlags(sample.hitSound ?? ''))
        : undefined;
      drawBodyMarker(ctx, x, bounds, color, additionColor);
    }
  }

  for (const marker of primaryEdgeMarkers) {
    const x = getTimelineX(marker.timeMs, startTimeMs, durationMs, width);
    if (x < visibleStartX - 4 || x > visibleEndX + 4) continue;

    drawEdgeMarker(ctx, x, bounds, getSamplesetColor(marker.sampleset));
  }
}

export function filterSamplesForHover(
  samples: ObjectsTimelineSample[],
  layers: HitsoundLayerVisibility
): ObjectsTimelineSample[] {
  const filtered = samples.filter((sample) => {
    if (sample.source === 'Edge') return true;
    if (sample.source === 'Body') return layers.body;
    if (sample.source === 'Tick') return layers.ticks;
    return false;
  });

  return dedupePassiveBodySamples(filtered);
}
