import {
  SAMPLESET_ACCENT_ALPHA,
  SAMPLESET_OVERLAY_ALPHA,
  SAMPLESET_TINTS,
  getSampleColor,
  type HitsoundLayerVisibility,
} from './hitsoundUtils.ts';
import { getTimelineX, getObjectBodyWidth } from './timelineUtils.ts';
import { withAlpha } from '../../../utils/color.ts';
import type {
  ObjectsHitsoundGapPeriod,
  ObjectsOverviewDifficulty,
  ObjectsTimelineSample,
  ObjectsTimingSegment,
} from '../../../Types';
import type { MantineTheme } from '@mantine/core';

export const SOUND_STRIP_TOP_OFFSET = 6;
export const SOUND_STRIP_EDGE_LANE_HEIGHT = 16;
export const SOUND_STRIP_PASSIVE_LANE_HEIGHT = 10;
export const SOUND_STRIP_LANE_GAP = 2;
export const EDGE_MARKER_WIDTH = 3;
export const BODY_MARKER_WIDTH = 6;
export const BODY_MARKER_HEIGHT = 4;
export const TICK_MARKER_RADIUS = 2.5;

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
    theme,
  }: {
    gapPeriods: ObjectsHitsoundGapPeriod[];
    startTimeMs: number;
    durationMs: number;
    width: number;
    visibleStartX: number;
    visibleEndX: number;
    height: number;
    theme: MantineTheme;
  }
) {
  for (const gap of gapPeriods) {
    if (gap.endTimeMs <= gap.startTimeMs) continue;

    const startX = getTimelineX(gap.startTimeMs, startTimeMs, durationMs, width);
    const endX = getTimelineX(gap.endTimeMs, startTimeMs, durationMs, width);
    const bounds = getObjectBodyWidth(startX, endX, visibleStartX, visibleEndX, 4);
    if (!bounds) continue;

    ctx.save();
    ctx.fillStyle = withAlpha(theme.colors.orange[8], 0.12);
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
    if (sampleset === 'Normal' || sampleset === 'Auto') continue;

    const segStart = Math.max(startTimeMs, segment.startTimeMs);
    const segEnd = Math.min(endTimeMs, segment.endTimeMs);
    if (segEnd <= segStart) continue;

    const startX = getTimelineX(segStart, startTimeMs, durationMs, width);
    const endX = getTimelineX(segEnd, startTimeMs, durationMs, width);
    const bounds = getObjectBodyWidth(startX, endX, visibleStartX, visibleEndX, 2);
    if (!bounds) continue;

    const tint = SAMPLESET_TINTS[sampleset] ?? SAMPLESET_TINTS.Normal;
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
  const markerTop =
    bounds.passiveLaneTop + (bounds.passiveLaneHeight - BODY_MARKER_HEIGHT) / 2;

  ctx.fillStyle = color;
  ctx.fillRect(markerLeft, markerTop, BODY_MARKER_WIDTH, BODY_MARKER_HEIGHT);
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
  }: {
    difficulty: ObjectsOverviewDifficulty;
    layers: HitsoundLayerVisibility;
    startTimeMs: number;
    durationMs: number;
    width: number;
    visibleStartX: number;
    visibleEndX: number;
    height: number;
  }
) {
  const samples = difficulty.timelineSamples ?? [];
  const bounds = getSoundStripBounds(height);

  drawLaneDivider(ctx, bounds, visibleStartX, visibleEndX);

  const passiveSamples: ObjectsTimelineSample[] = [];
  const edgeSamples: ObjectsTimelineSample[] = [];

  for (const sample of samples) {
    if (sample.source === 'Edge') {
      edgeSamples.push(sample);
      continue;
    }
    if (sample.source === 'Body' && layers.body) {
      passiveSamples.push(sample);
      continue;
    }
    if (sample.source === 'Tick' && layers.ticks) {
      passiveSamples.push(sample);
    }
  }

  for (const sample of passiveSamples) {
    const x = getTimelineX(sample.timeMs, startTimeMs, durationMs, width);
    if (x < visibleStartX - 4 || x > visibleEndX + 4) continue;

    const color = withAlpha(getSampleColor(null, sample.source), 0.9);
    if (sample.source === 'Tick') {
      drawTickMarker(ctx, x, bounds, color);
    } else {
      drawBodyMarker(ctx, x, bounds, color);
    }
  }

  for (const sample of edgeSamples) {
    const x = getTimelineX(sample.timeMs, startTimeMs, durationMs, width);
    if (x < visibleStartX - 4 || x > visibleEndX + 4) continue;

    drawEdgeMarker(ctx, x, bounds, getSampleColor(sample.hitSound));
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
