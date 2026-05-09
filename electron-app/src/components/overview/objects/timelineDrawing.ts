import { REVERSE_ARROW_ICON_SIZE, TAIKO_DRUMROLL_COLOR, TAIKO_SPINNER_COLOR } from './constants.ts';
import {
  getAlignedTimelineLineX,
  getObjectBodyWidth,
  getSpinnerMarkerRadius,
  getTimelineObjectCircleRadius,
  getTimelineX,
  getTimingTickStyle,
  hasNearbyRoundedEdge,
} from './timelineUtils.ts';
import { withAlpha } from '../../../utils/color.ts';
import { normalizeMode } from '../../../utils/gameMode';
import type {
  Mode,
  ObjectsBreakPeriod,
  ObjectsOverviewDifficulty,
  ObjectsTimelineObject,
  ObjectsTimingSegment,
} from '../../../Types';
import type { MantineTheme } from '@mantine/core';

export function drawTimelineRow(
  ctx: CanvasRenderingContext2D,
  {
    difficulty,
    startTimeMs,
    endTimeMs,
    timelineWidth,
    viewportStartX,
    viewportWidth,
    height,
    theme,
  }: {
    difficulty: ObjectsOverviewDifficulty;
    startTimeMs: number;
    endTimeMs: number;
    timelineWidth: number;
    viewportStartX: number;
    viewportWidth: number;
    height: number;
    theme: MantineTheme;
  }
) {
  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const centerY = height / 2;
  const viewportEndX = viewportStartX + viewportWidth;

  ctx.clearRect(0, 0, viewportWidth, height);
  ctx.save();
  ctx.beginPath();
  ctx.rect(0, 0, viewportWidth, height);
  ctx.clip();
  ctx.translate(-viewportStartX, 0);

  ctx.fillStyle = theme.colors.dark[7];
  ctx.fillRect(viewportStartX, 0, viewportWidth, height);

  drawBreakPeriods(ctx, {
    breakPeriods: difficulty.breakPeriods,
    startTimeMs,
    durationMs,
    width: timelineWidth,
    visibleStartX: viewportStartX,
    visibleEndX: viewportEndX,
    height,
    theme,
  });

  drawTimingGrid(ctx, {
    timingSegments: difficulty.timingSegments,
    timelineObjects: difficulty.timelineObjects,
    startTimeMs,
    endTimeMs,
    durationMs,
    width: timelineWidth,
    visibleStartX: viewportStartX,
    visibleEndX: viewportEndX,
    height,
  });

  ctx.strokeStyle = withAlpha(theme.colors.dark[2], 0.35);
  ctx.lineWidth = 1;
  ctx.beginPath();
  ctx.moveTo(viewportStartX, centerY);
  ctx.lineTo(viewportEndX, centerY);
  ctx.stroke();

  const difficultyMode = normalizeMode(difficulty.mode);

  for (const timelineObject of difficulty.timelineObjects) {
    drawTimelineObject(
      ctx,
      timelineObject,
      difficultyMode,
      startTimeMs,
      durationMs,
      timelineWidth,
      centerY,
      viewportStartX,
      viewportEndX
    );
  }

  const drawnMarkers = new Set<string>();
  for (const timelineObject of difficulty.timelineObjects) {
    if (timelineObject.objectType === 'Circle') {
      continue;
    }

    for (const edge of timelineObject.edges) {
      const rawX = getTimelineX(edge.timeMs, startTimeMs, durationMs, timelineWidth);
      if (rawX < viewportStartX || rawX > viewportEndX) continue;

      const x = getAlignedTimelineLineX(edge.timeMs, startTimeMs, durationMs, timelineWidth);
      const markerKey = `${timelineObject.objectType}-${edge.partName}-${x}`;
      if (drawnMarkers.has(markerKey)) continue;

      drawnMarkers.add(markerKey);
      drawObjectMarker(ctx, timelineObject, difficultyMode, edge.partName, x, centerY);
    }
  }

  ctx.restore();
}

export function getTimelineTimestampAtX({
  difficulty,
  startTimeMs,
  endTimeMs,
  timelineWidth,
  x,
}: {
  difficulty: ObjectsOverviewDifficulty;
  startTimeMs: number;
  endTimeMs: number;
  timelineWidth: number;
  x: number;
}): number | null {
  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const difficultyMode = normalizeMode(difficulty.mode);
  let bestTimeMs: number | null = null;
  let bestDistance = Number.POSITIVE_INFINITY;

  const consider = (timeMs: number, distance: number, threshold: number) => {
    if (distance > threshold) return;
    if (distance < bestDistance) {
      bestDistance = distance;
      bestTimeMs = timeMs;
    }
  };

  for (const timelineObject of difficulty.timelineObjects) {
    if (timelineObject.objectType === 'Circle') {
      const centerX = getTimelineX(
        timelineObject.startTimeMs,
        startTimeMs,
        durationMs,
        timelineWidth
      );
      const radius = getTimelineObjectCircleRadius(difficultyMode, timelineObject);
      consider(timelineObject.startTimeMs, Math.abs(x - centerX), radius + 4);
      continue;
    }

    const startX = getTimelineX(timelineObject.startTimeMs, startTimeMs, durationMs, timelineWidth);
    const endX = getTimelineX(timelineObject.endTimeMs, startTimeMs, durationMs, timelineWidth);
    const minX = Math.min(startX, endX);
    const maxX = Math.max(startX, endX);
    const distanceToBody =
      x >= minX && x <= maxX ? 0 : Math.min(Math.abs(x - minX), Math.abs(x - maxX));
    const bodyTimeMs =
      startX === endX
        ? timelineObject.startTimeMs
        : timelineObject.startTimeMs +
          ((x - startX) / (endX - startX)) *
            (timelineObject.endTimeMs - timelineObject.startTimeMs);
    consider(bodyTimeMs, distanceToBody, 8);

    for (const edge of timelineObject.edges) {
      const edgeX = getTimelineX(edge.timeMs, startTimeMs, durationMs, timelineWidth);
      consider(edge.timeMs, Math.abs(x - edgeX), 8);
    }
  }

  if (bestTimeMs === null) {
    return null;
  }

  return Math.max(startTimeMs, Math.min(endTimeMs, bestTimeMs));
}

function drawBreakPeriods(
  ctx: CanvasRenderingContext2D,
  {
    breakPeriods,
    startTimeMs,
    durationMs,
    width,
    visibleStartX,
    visibleEndX,
    height,
    theme,
  }: {
    breakPeriods: ObjectsBreakPeriod[];
    startTimeMs: number;
    durationMs: number;
    width: number;
    visibleStartX: number;
    visibleEndX: number;
    height: number;
    theme: MantineTheme;
  }
) {
  if (breakPeriods.length === 0) return;

  const blockY = 4;
  const blockHeight = Math.max(0, height - 8);

  for (const breakPeriod of breakPeriods) {
    if (breakPeriod.endTimeMs <= breakPeriod.startTimeMs) continue;

    const startX = getTimelineX(breakPeriod.startTimeMs, startTimeMs, durationMs, width);
    const endX = getTimelineX(breakPeriod.endTimeMs, startTimeMs, durationMs, width);
    const bounds = getObjectBodyWidth(startX, endX, visibleStartX, visibleEndX, 12);
    if (!bounds) continue;

    const blockWidth = bounds.endX - bounds.startX;

    ctx.save();
    ctx.fillStyle = withAlpha(theme.colors.yellow[7], 0.08);
    ctx.fillRect(bounds.startX, blockY, blockWidth, blockHeight);

    ctx.setLineDash([5, 4]);
    ctx.strokeStyle = withAlpha(theme.colors.yellow[4], 0.45);
    ctx.lineWidth = 1;
    ctx.strokeRect(
      bounds.startX + 0.5,
      blockY + 0.5,
      Math.max(0, blockWidth - 1),
      Math.max(0, blockHeight - 1)
    );
    ctx.setLineDash([]);

    if (blockWidth >= 20) {
      drawBreakPauseIcon(ctx, bounds.startX + blockWidth / 2, height / 2, theme.colors.yellow[2]);
    }

    ctx.restore();
  }
}

function drawTimingGrid(
  ctx: CanvasRenderingContext2D,
  {
    timingSegments,
    timelineObjects,
    startTimeMs,
    endTimeMs,
    durationMs,
    width,
    visibleStartX,
    visibleEndX,
    height,
  }: {
    timingSegments: ObjectsTimingSegment[];
    timelineObjects: ObjectsTimelineObject[];
    startTimeMs: number;
    endTimeMs: number;
    durationMs: number;
    width: number;
    visibleStartX: number;
    visibleEndX: number;
    height: number;
  }
) {
  if (timingSegments.length === 0) return;

  const roundedEdgeTimes = new Set(
    timelineObjects.flatMap((timelineObject) =>
      timelineObject.edges.map((edge) => Math.round(edge.timeMs))
    )
  );
  const tickLines = new Map<
    string,
    { x: number; color: string; height: number; alpha: number; priority: number }
  >();

  for (const segment of timingSegments) {
    const visibleStartMs = Math.max(startTimeMs, segment.startTimeMs);
    const visibleEndMs = Math.min(endTimeMs, segment.endTimeMs);
    const sampleStepMs = segment.msPerBeat / 48;

    if (visibleEndMs <= visibleStartMs || sampleStepMs <= 0) continue;

    const startSampleIndex = Math.max(
      0,
      Math.ceil((visibleStartMs - segment.offsetMs) / sampleStepMs)
    );
    const endSampleIndex = Math.floor((visibleEndMs - segment.offsetMs) / sampleStepMs);

    for (let sampleIndex = startSampleIndex; sampleIndex <= endSampleIndex; sampleIndex += 1) {
      const sampleTimeMs = segment.offsetMs + sampleIndex * sampleStepMs;
      const rawX = getTimelineX(sampleTimeMs, startTimeMs, durationMs, width);
      if (rawX < visibleStartX || rawX > visibleEndX) continue;

      const hasNearbyEdge = hasNearbyRoundedEdge(roundedEdgeTimes, sampleTimeMs);
      const tickStyle = getTimingTickStyle(sampleIndex, segment.meter, hasNearbyEdge);
      if (!tickStyle) continue;

      const x = getAlignedTimelineLineX(sampleTimeMs, startTimeMs, durationMs, width);
      const key = x.toFixed(1);
      const existing = tickLines.get(key);

      if (!existing || tickStyle.priority >= existing.priority) {
        tickLines.set(key, { x, ...tickStyle });
      }
    }
  }

  const tickBottomY = height - 6;
  for (const tickLine of Array.from(tickLines.values()).sort((left, right) => left.x - right.x)) {
    ctx.strokeStyle = withAlpha(tickLine.color, tickLine.alpha);
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(tickLine.x, tickBottomY - tickLine.height);
    ctx.lineTo(tickLine.x, tickBottomY);
    ctx.stroke();
  }
}

function drawTimelineObject(
  ctx: CanvasRenderingContext2D,
  timelineObject: ObjectsTimelineObject,
  difficultyMode: Mode,
  startTimeMs: number,
  durationMs: number,
  width: number,
  centerY: number,
  visibleStartX: number,
  visibleEndX: number
) {
  const color = getTimelineObjectColor(difficultyMode, timelineObject);
  const circleRadius = getTimelineObjectCircleRadius(difficultyMode, timelineObject);

  if (timelineObject.objectType === 'Circle') {
    const x = getTimelineX(timelineObject.startTimeMs, startTimeMs, durationMs, width);
    if (x < visibleStartX - (circleRadius + 2) || x > visibleEndX + circleRadius + 2) {
      return;
    }

    drawCircleObject(ctx, x, centerY, color, circleRadius);
    return;
  }

  drawObjectBody(
    ctx,
    timelineObject,
    difficultyMode,
    startTimeMs,
    durationMs,
    width,
    centerY,
    visibleStartX,
    visibleEndX
  );
}

function drawObjectBody(
  ctx: CanvasRenderingContext2D,
  timelineObject: ObjectsTimelineObject,
  difficultyMode: Mode,
  startTimeMs: number,
  durationMs: number,
  width: number,
  centerY: number,
  visibleStartX: number,
  visibleEndX: number
) {
  if (timelineObject.endTimeMs <= timelineObject.startTimeMs) return;

  const startX = getTimelineX(timelineObject.startTimeMs, startTimeMs, durationMs, width);
  const endX = getTimelineX(timelineObject.endTimeMs, startTimeMs, durationMs, width);
  const bodyBounds = getObjectBodyWidth(
    startX,
    endX,
    visibleStartX,
    visibleEndX,
    timelineObject.objectType === 'Spinner' ? 12 : 8
  );
  if (!bodyBounds) return;

  const color = getTimelineObjectColor(difficultyMode, timelineObject);

  if (timelineObject.objectType === 'Spinner') {
    ctx.strokeStyle = withAlpha(color, 0.18);
    ctx.lineWidth = 32;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.moveTo(bodyBounds.startX, centerY);
    ctx.lineTo(bodyBounds.endX, centerY);
    ctx.stroke();

    ctx.strokeStyle = withAlpha(color, 0.45);
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.moveTo(bodyBounds.startX, centerY);
    ctx.lineTo(bodyBounds.endX, centerY);
    ctx.stroke();
    return;
  }

  if (timelineObject.objectType === 'Hold note') {
    ctx.fillStyle = withAlpha(color, 0.42);
    ctx.fillRect(bodyBounds.startX, centerY - 5, bodyBounds.endX - bodyBounds.startX, 10);
    ctx.strokeStyle = withAlpha(color, 0.85);
    ctx.lineWidth = 1.5;
    ctx.strokeRect(bodyBounds.startX, centerY - 5, bodyBounds.endX - bodyBounds.startX, 10);
    return;
  }

  ctx.strokeStyle = withAlpha(color, 0.4);
  ctx.lineWidth = 32;
  ctx.lineCap = 'round';
  ctx.beginPath();
  ctx.moveTo(bodyBounds.startX, centerY);
  ctx.lineTo(bodyBounds.endX, centerY);
  ctx.stroke();

  ctx.strokeStyle = withAlpha(color, 0.95);
  ctx.lineWidth = 2;
  ctx.beginPath();
  ctx.moveTo(bodyBounds.startX, centerY);
  ctx.lineTo(bodyBounds.endX, centerY);
  ctx.stroke();
}

function drawObjectMarker(
  ctx: CanvasRenderingContext2D,
  timelineObject: ObjectsTimelineObject,
  difficultyMode: Mode,
  partName: string,
  x: number,
  centerY: number
) {
  const lowerPart = partName.toLowerCase();
  const color = getTimelineObjectColor(difficultyMode, timelineObject);
  const isSlider = timelineObject.objectType === 'Slider';
  const circleRadius = getTimelineObjectCircleRadius(difficultyMode, timelineObject);

  if (timelineObject.objectType === 'Spinner') {
    drawSpinnerIcon(ctx, x, centerY, color, getSpinnerMarkerRadius(difficultyMode));
    return;
  }

  ctx.strokeStyle = withAlpha(color, 0.95);
  ctx.fillStyle = withAlpha(color, 0.95);
  ctx.lineWidth = 1.5;

  if (lowerPart.includes('reverse')) {
    drawCircleObject(ctx, x, centerY, color, circleRadius);
    drawReverseArrowIcon(ctx, x, centerY);
    return;
  }

  if (lowerPart.includes('tail') || lowerPart.includes('end')) {
    if (isSlider) {
      drawCircleObject(ctx, x, centerY, color, circleRadius);
      return;
    }

    ctx.fillRect(x - 3, centerY - 3, 6, 6);
    return;
  }

  drawCircleObject(ctx, x, centerY, color, circleRadius);
}

function drawCircleObject(
  ctx: CanvasRenderingContext2D,
  x: number,
  centerY: number,
  color: string,
  radius: number
) {
  ctx.beginPath();
  ctx.fillStyle = withAlpha(color, 0.55);
  ctx.strokeStyle = withAlpha(color, 0.98);
  ctx.lineWidth = 1.75;
  ctx.arc(x, centerY, radius, 0, Math.PI * 2);
  ctx.fill();
  ctx.stroke();
}

function drawReverseArrowIcon(
  ctx: CanvasRenderingContext2D,
  x: number,
  centerY: number,
  size = REVERSE_ARROW_ICON_SIZE
) {
  const leftX = x - size;
  const middleX = x - size * 0.17;
  const rightX = x + size * 0.67;
  const yOffset = size * 0.89;

  ctx.save();
  ctx.strokeStyle = 'rgba(255,255,255,0.95)';
  ctx.lineWidth = Math.max(1.4, size * 0.36);
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';

  ctx.beginPath();
  ctx.moveTo(leftX, centerY - yOffset);
  ctx.lineTo(middleX, centerY);
  ctx.lineTo(leftX, centerY + yOffset);
  ctx.stroke();

  ctx.beginPath();
  ctx.moveTo(middleX, centerY - yOffset);
  ctx.lineTo(rightX, centerY);
  ctx.lineTo(middleX, centerY + yOffset);
  ctx.stroke();

  ctx.restore();
}

function drawSpinnerIcon(
  ctx: CanvasRenderingContext2D,
  x: number,
  centerY: number,
  color: string,
  radius: number
) {
  const ringRadius = Math.max(radius * 0.72, radius - 4);
  const accentX = x + ringRadius * 0.55;
  const accentY = centerY - ringRadius * 0.6;

  ctx.save();
  ctx.beginPath();
  ctx.fillStyle = withAlpha(color, 0.22);
  ctx.strokeStyle = withAlpha(color, 0.9);
  ctx.lineWidth = Math.max(1.25, radius * 0.1);
  ctx.arc(x, centerY, radius, 0, Math.PI * 2);
  ctx.fill();
  ctx.stroke();

  ctx.beginPath();
  ctx.strokeStyle = 'rgba(255,255,255,0.95)';
  ctx.lineWidth = Math.max(1.75, radius * 0.16);
  ctx.lineCap = 'round';
  ctx.arc(x, centerY, ringRadius, -Math.PI * 0.35, Math.PI * 1.15);
  ctx.stroke();

  ctx.beginPath();
  ctx.fillStyle = 'rgba(255,255,255,0.95)';
  ctx.arc(accentX, accentY, Math.max(1.4, radius * 0.12), 0, Math.PI * 2);
  ctx.fill();

  ctx.beginPath();
  ctx.fillStyle = withAlpha(color, 0.92);
  ctx.arc(x, centerY, Math.max(1.6, radius * 0.15), 0, Math.PI * 2);
  ctx.fill();
  ctx.restore();
}

function drawBreakPauseIcon(
  ctx: CanvasRenderingContext2D,
  x: number,
  centerY: number,
  color: string
) {
  ctx.save();
  ctx.fillStyle = withAlpha(color, 0.9);
  ctx.fillRect(x - 4, centerY - 5, 2.5, 10);
  ctx.fillRect(x + 1.5, centerY - 5, 2.5, 10);
  ctx.restore();
}

function getTimelineObjectColor(difficultyMode: Mode, timelineObject: ObjectsTimelineObject) {
  if (difficultyMode === 'Taiko') {
    if (timelineObject.objectType === 'Spinner') {
      return TAIKO_SPINNER_COLOR;
    }

    if (timelineObject.objectType === 'Slider') {
      return TAIKO_DRUMROLL_COLOR;
    }
  }

  return timelineObject.comboColourHex ?? '#ced4da';
}
