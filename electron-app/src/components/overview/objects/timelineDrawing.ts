import { REVERSE_ARROW_ICON_SIZE, SLIDER_TICK_DOT_RADIUS } from './constants.ts';
import {
  buildHitsoundDrawCache,
  getDominantHitsoundColor,
  getHitsoundCircleLayout,
  getHitsoundCircleOuterRadius,
  getSamplesetColor,
  getSecondaryHitsoundColors,
  SAMPLESET_BODY_ALPHA,
  type HitsoundLayerVisibility,
  type TimelineViewMode,
} from './hitsoundUtils.ts';
import {
  drawHitsoundGapOverlay,
  drawSamplesetRegions,
  drawSoundStrip,
} from './timelineHitsoundDrawing.ts';
import {
  drawThemedCircle,
  drawThemedHoldNote,
  drawThemedLineBody,
  drawThemedObjectMarkerEdge,
  drawThemedReverseArrow,
  drawThemedSliderEndpoint,
  drawThemedSpinnerMarker,
  drawThemedTailSquare,
} from './timelineTheme/apply.ts';
import { resolveTimelineVisualTheme } from './timelineTheme/selection.ts';
import {
  buildRoundedEdgeTimes,
  findLowerBoundIndex,
  getAlignedTimelineLineX,
  getObjectBodyWidth,
  getTimelineTimeFromX,
  getTimelineX,
  getTimingTickStyle,
  hasNearbyRoundedEdge,
  type TimelineRowDrawCache,
} from './timelineUtils.ts';
import { withAlpha } from '../../../utils/color.ts';
import type { TimelineThemeVariant, TimelineVisualTheme } from './timelineTheme/types.ts';
import type {
  ObjectsBreakPeriod,
  ObjectsOverviewDifficulty,
  ObjectsTimelineEdge,
  ObjectsTimelineObject,
  ObjectsTimelineSample,
  ObjectsTimingSegment,
} from '../../../Types';
import type { MantineTheme } from '@mantine/core';

const SLIDER_TICK_DOT_ALPHA = 0.65;
// Visibility-check padding for markers/ticks near a tile's edge — must cover the largest radius
// a marker (slider head/tail circle, reverse arrow) is drawn at, so both neighboring tiles
// attempt to draw a boundary-straddling marker and their clips split it correctly (as already
// happens for plain hit circles in drawTimelineObject).
const MARKER_CULL_PADDING_PX = 40;
const TICK_CULL_PADDING_PX = 8;

export type DrawTimelineRowOptions = {
  difficulty: ObjectsOverviewDifficulty;
  startTimeMs: number;
  endTimeMs: number;
  timelineWidth: number;
  viewportStartX: number;
  viewportWidth: number;
  height: number;
  theme: MantineTheme;
  visualThemeVariant: TimelineThemeVariant;
  viewMode?: TimelineViewMode;
  hitsoundLayers?: HitsoundLayerVisibility;
  rowDrawCache?: TimelineRowDrawCache;
};

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
    visualThemeVariant,
    viewMode = 'structure',
    hitsoundLayers,
    rowDrawCache,
  }: DrawTimelineRowOptions
) {
  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const centerY = height / 2;
  const viewportEndX = viewportStartX + viewportWidth;
  const visualTheme = resolveTimelineVisualTheme(difficulty.mode, visualThemeVariant);
  const isHitsoundView = viewMode === 'hitsounding';
  const layers = hitsoundLayers ?? {
    body: true,
    ticks: true,
    sampleset: true,
    gaps: true,
  };
  const neutralBodyColor = theme.colors.dark[4];
  const roundedEdgeTimes =
    rowDrawCache?.roundedEdgeTimes ?? buildRoundedEdgeTimes(difficulty.timelineObjects);
  const resolvedHitsoundCache = isHitsoundView
    ? (rowDrawCache?.hitsound ??
      buildHitsoundDrawCache(difficulty.timelineObjects, difficulty.timelineSamples ?? []))
    : undefined;

  ctx.clearRect(0, 0, viewportWidth, height);
  ctx.save();
  ctx.beginPath();
  ctx.rect(0, 0, viewportWidth, height);
  ctx.clip();
  ctx.translate(-viewportStartX, 0);

  ctx.fillStyle = theme.colors.dark[7];
  ctx.fillRect(viewportStartX, 0, viewportWidth, height);

  if (isHitsoundView && layers.gaps) {
    drawHitsoundGapOverlay(ctx, {
      gapPeriods: difficulty.hitsoundGapPeriods ?? [],
      startTimeMs,
      durationMs,
      width: timelineWidth,
      visibleStartX: viewportStartX,
      visibleEndX: viewportEndX,
      height,
    });
  }

  if (isHitsoundView && layers.sampleset) {
    drawSamplesetRegions(ctx, {
      timingSegments: difficulty.timingSegments,
      startTimeMs,
      endTimeMs,
      durationMs,
      width: timelineWidth,
      visibleStartX: viewportStartX,
      visibleEndX: viewportEndX,
      height,
    });
  }

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
    roundedEdgeTimes,
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

  const { timelineObjects } = difficulty;
  const drawnMarkers = new Set<string>();
  const timelineSamples = difficulty.timelineSamples ?? [];
  const sliderTickSamples = rowDrawCache?.sortedSliderTickSamples ?? timelineSamples;

  // Draw only objects overlapping this tile's viewport instead of scanning every object in the
  // difficulty — the extra margin covers visual radius/marker overhang plus the longest
  // slider/spinner that could start just before the window and still extend into it.
  const CULL_MARGIN_PX = 96;
  const windowStartMs = getTimelineTimeFromX(
    viewportStartX - CULL_MARGIN_PX,
    startTimeMs,
    durationMs,
    timelineWidth
  );
  const windowEndMs = getTimelineTimeFromX(
    viewportEndX + CULL_MARGIN_PX,
    startTimeMs,
    durationMs,
    timelineWidth
  );

  let drawObjects: ObjectsTimelineObject[];
  let drawStartIndex: number;
  let drawEndIndexExclusive: number;

  if (rowDrawCache) {
    const { sortedObjects, maxObjectDurationMs } = rowDrawCache;
    drawObjects = sortedObjects;
    drawStartIndex = findLowerBoundIndex(
      sortedObjects,
      windowStartMs - maxObjectDurationMs,
      (object) => object.startTimeMs
    );
    drawEndIndexExclusive = findLowerBoundIndex(
      sortedObjects,
      windowEndMs + 1e-6,
      (object) => object.startTimeMs
    );
  } else {
    drawObjects = timelineObjects;
    drawStartIndex = 0;
    drawEndIndexExclusive = timelineObjects.length;
  }

  for (let index = drawEndIndexExclusive - 1; index >= drawStartIndex; index -= 1) {
    const timelineObject = drawObjects[index];
    drawTimelineObject(
      ctx,
      timelineObject,
      visualTheme,
      startTimeMs,
      durationMs,
      timelineWidth,
      centerY,
      viewportStartX,
      viewportEndX,
      isHitsoundView,
      neutralBodyColor,
      resolvedHitsoundCache?.bodySampleByObject.get(timelineObject) ?? null
    );

    if (!isHitsoundView && timelineObject.objectType === 'Slider') {
      drawSliderTickDots(ctx, {
        samples: sliderTickSamples,
        startTimeMs,
        durationMs,
        timelineWidth,
        centerY,
        visibleStartX: viewportStartX,
        visibleEndX: viewportEndX,
        startTimeFilterMs: timelineObject.startTimeMs,
        endTimeFilterMs: timelineObject.endTimeMs,
      });
    }

    if (timelineObject.objectType !== 'Circle') {
      drawTimelineObjectMarkers(
        ctx,
        timelineObject,
        visualTheme,
        startTimeMs,
        durationMs,
        timelineWidth,
        centerY,
        viewportStartX,
        viewportEndX,
        isHitsoundView,
        neutralBodyColor,
        drawnMarkers
      );
    }
  }

  if (isHitsoundView && resolvedHitsoundCache) {
    drawSoundStrip(ctx, {
      difficulty,
      layers,
      startTimeMs,
      durationMs,
      width: timelineWidth,
      visibleStartX: viewportStartX,
      visibleEndX: viewportEndX,
      height,
      primaryEdgeMarkers: resolvedHitsoundCache.primaryEdgeMarkers,
    });
  }

  ctx.restore();
}

function interpolateTimelineBodyTimeMs(
  timelineObject: ObjectsTimelineObject,
  x: number,
  startX: number,
  endX: number
): number {
  if (startX === endX) {
    return timelineObject.startTimeMs;
  }

  return (
    timelineObject.startTimeMs +
    ((x - startX) / (endX - startX)) * (timelineObject.endTimeMs - timelineObject.startTimeMs)
  );
}

/** Nearest slider head, reverse, or tail by horizontal distance (right-click snap). */
function getSliderSnapTimeMsAtX(
  timelineObject: ObjectsTimelineObject,
  x: number,
  startTimeMs: number,
  durationMs: number,
  timelineWidth: number
): number {
  let bestTimeMs = timelineObject.startTimeMs;
  let bestDistance = Number.POSITIVE_INFINITY;

  for (const edge of timelineObject.edges) {
    if (!isHoverableTimelineEdge(timelineObject.objectType, edge.partName)) {
      continue;
    }

    const edgeX = getTimelineX(edge.timeMs, startTimeMs, durationMs, timelineWidth);
    const distance = Math.abs(x - edgeX);
    if (distance < bestDistance) {
      bestDistance = distance;
      bestTimeMs = edge.timeMs;
    }
  }

  if (
    timelineObject.edges.some((edge) =>
      isHoverableTimelineEdge(timelineObject.objectType, edge.partName)
    )
  ) {
    return bestTimeMs;
  }

  const startX = getTimelineX(timelineObject.startTimeMs, startTimeMs, durationMs, timelineWidth);
  const endX = getTimelineX(timelineObject.endTimeMs, startTimeMs, durationMs, timelineWidth);
  const distToStart = Math.abs(x - startX);
  const distToEnd = Math.abs(x - endX);
  return distToStart <= distToEnd ? timelineObject.startTimeMs : timelineObject.endTimeMs;
}

export function getTimelineTimestampAtX({
  difficulty,
  startTimeMs,
  endTimeMs,
  timelineWidth,
  x,
  visualThemeVariant,
}: {
  difficulty: ObjectsOverviewDifficulty;
  startTimeMs: number;
  endTimeMs: number;
  timelineWidth: number;
  x: number;
  visualThemeVariant: TimelineThemeVariant;
}): number | null {
  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const visualTheme = resolveTimelineVisualTheme(difficulty.mode, visualThemeVariant);
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
      const radius = visualTheme.circleRadius(timelineObject);
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
      timelineObject.objectType === 'Slider'
        ? getSliderSnapTimeMsAtX(timelineObject, x, startTimeMs, durationMs, timelineWidth)
        : interpolateTimelineBodyTimeMs(timelineObject, x, startX, endX);
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

export type TimelineObjectHeadHit = {
  object: ObjectsTimelineObject;
  edge: ObjectsTimelineEdge | null;
  timeMs: number;
  anchorX: number;
  partLabel: string;
  showSnapLabel: boolean;
};

function isHoverableTimelineEdge(objectType: string, partName: string) {
  const lower = partName.toLowerCase();

  if (lower.includes('head')) {
    return true;
  }

  if (objectType === 'Slider') {
    return (
      lower.includes('reverse') ||
      lower.includes('repeat') ||
      lower.includes('tail') ||
      lower.includes('end')
    );
  }

  if (objectType === 'Spinner') {
    return lower.includes('tail') || lower.includes('end');
  }

  return false;
}

function getTimelineMarkerHitThreshold(
  timelineObject: ObjectsTimelineObject,
  visualTheme: ReturnType<typeof resolveTimelineVisualTheme>
) {
  if (timelineObject.objectType === 'Spinner') {
    return visualTheme.spinnerMarkerRadius + 4;
  }

  return visualTheme.circleRadius(timelineObject) + 4;
}

function getNearestHoverableMarkerDistanceAtX(
  timelineObject: ObjectsTimelineObject,
  x: number,
  startTimeMs: number,
  durationMs: number,
  timelineWidth: number
): number {
  let nearest = Number.POSITIVE_INFINITY;

  for (const edge of timelineObject.edges) {
    if (!isHoverableTimelineEdge(timelineObject.objectType, edge.partName)) {
      continue;
    }

    const centerX = getTimelineX(edge.timeMs, startTimeMs, durationMs, timelineWidth);
    nearest = Math.min(nearest, Math.abs(x - centerX));
  }

  return nearest;
}

export function findTimelineObjectHeadAtX({
  difficulty,
  startTimeMs,
  endTimeMs,
  timelineWidth,
  x,
  visualThemeVariant,
}: {
  difficulty: ObjectsOverviewDifficulty;
  startTimeMs: number;
  endTimeMs: number;
  timelineWidth: number;
  x: number;
  visualThemeVariant: TimelineThemeVariant;
}): TimelineObjectHeadHit | null {
  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const visualTheme = resolveTimelineVisualTheme(difficulty.mode, visualThemeVariant);
  let bestHit: TimelineObjectHeadHit | null = null;
  let bestDistance = Number.POSITIVE_INFINITY;

  const consider = (
    object: ObjectsTimelineObject,
    edge: ObjectsTimelineEdge | null,
    timeMs: number,
    centerX: number,
    threshold: number,
    partLabel: string,
    showSnapLabel = true,
    hitDistance = Math.abs(x - centerX)
  ) => {
    if (hitDistance > threshold || hitDistance >= bestDistance) {
      return;
    }

    bestDistance = hitDistance;
    bestHit = { object, edge, timeMs, anchorX: centerX, partLabel, showSnapLabel };
  };

  for (const timelineObject of difficulty.timelineObjects) {
    if (timelineObject.objectType === 'Circle') {
      const centerX = getTimelineX(
        timelineObject.startTimeMs,
        startTimeMs,
        durationMs,
        timelineWidth
      );
      const radius = visualTheme.circleRadius(timelineObject);
      consider(timelineObject, null, timelineObject.startTimeMs, centerX, radius + 4, 'Circle');
      continue;
    }

    for (const edge of timelineObject.edges) {
      if (!isHoverableTimelineEdge(timelineObject.objectType, edge.partName)) {
        continue;
      }

      const centerX = getTimelineX(edge.timeMs, startTimeMs, durationMs, timelineWidth);
      const threshold = getTimelineMarkerHitThreshold(timelineObject, visualTheme);
      consider(timelineObject, edge, edge.timeMs, centerX, threshold, edge.partName);
    }

    if (timelineObject.endTimeMs <= timelineObject.startTimeMs) {
      continue;
    }

    const startX = getTimelineX(timelineObject.startTimeMs, startTimeMs, durationMs, timelineWidth);
    const endX = getTimelineX(timelineObject.endTimeMs, startTimeMs, durationMs, timelineWidth);
    const minX = Math.min(startX, endX);
    const maxX = Math.max(startX, endX);
    const distanceToBody =
      x >= minX && x <= maxX ? 0 : Math.min(Math.abs(x - minX), Math.abs(x - maxX));

    if (distanceToBody > 8) {
      continue;
    }

    const markerThreshold = getTimelineMarkerHitThreshold(timelineObject, visualTheme);
    const nearestMarkerDistance = getNearestHoverableMarkerDistanceAtX(
      timelineObject,
      x,
      startTimeMs,
      durationMs,
      timelineWidth
    );
    // Inside the span, body distance is 0 and would beat every marker edge globally.
    // Inflate it when a head/repeat/tail on this object is close so markers keep priority.
    const bodyHitDistance =
      nearestMarkerDistance <= markerThreshold ? markerThreshold + 0.5 : distanceToBody;

    const bodyTimeMs = interpolateTimelineBodyTimeMs(timelineObject, x, startX, endX);
    const bodyPartLabel =
      timelineObject.objectType === 'Slider'
        ? 'Slider body'
        : timelineObject.objectType === 'Spinner'
          ? 'Spinner body'
          : 'Hold note body';

    consider(
      timelineObject,
      null,
      bodyTimeMs,
      x,
      8,
      bodyPartLabel,
      timelineObject.objectType !== 'Slider',
      bodyHitDistance
    );
  }

  return bestHit;
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
    roundedEdgeTimes,
    startTimeMs,
    endTimeMs,
    durationMs,
    width,
    visibleStartX,
    visibleEndX,
    height,
  }: {
    timingSegments: ObjectsTimingSegment[];
    roundedEdgeTimes: Set<number>;
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

  // Bound beat-sample generation to this tile's own window, not the full map duration — without
  // this, every tile would regenerate the whole map's worth of beat-tick samples just to keep
  // the handful that land within its small visible range.
  const tileWindowStartMs = Math.max(
    startTimeMs,
    getTimelineTimeFromX(visibleStartX - TICK_CULL_PADDING_PX, startTimeMs, durationMs, width)
  );
  const tileWindowEndMs = Math.min(
    endTimeMs,
    getTimelineTimeFromX(visibleEndX + TICK_CULL_PADDING_PX, startTimeMs, durationMs, width)
  );

  const tickLines = new Map<
    string,
    { x: number; color: string; height: number; alpha: number; priority: number }
  >();

  for (const segment of timingSegments) {
    const visibleStartMs = Math.max(tileWindowStartMs, segment.startTimeMs);
    const visibleEndMs = Math.min(tileWindowEndMs, segment.endTimeMs);
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
      if (rawX < visibleStartX - TICK_CULL_PADDING_PX || rawX > visibleEndX + TICK_CULL_PADDING_PX)
        continue;

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

function drawHitsoundCircle(
  ctx: CanvasRenderingContext2D,
  x: number,
  centerY: number,
  radius: number,
  hitSoundFlags: number
) {
  const fillColor = getDominantHitsoundColor(hitSoundFlags);
  const secondaryColors = getSecondaryHitsoundColors(hitSoundFlags);
  const layout = getHitsoundCircleLayout(radius, hitSoundFlags);

  ctx.save();
  ctx.beginPath();
  ctx.fillStyle = withAlpha(fillColor, 0.85);
  ctx.strokeStyle = withAlpha('#ffffff', 0.35);
  ctx.lineWidth = 1;
  ctx.arc(x, centerY, layout.fillRadius, 0, Math.PI * 2);
  ctx.fill();
  ctx.stroke();

  secondaryColors.forEach((secondaryColor, index) => {
    ctx.beginPath();
    ctx.strokeStyle = withAlpha(secondaryColor, 0.95);
    ctx.lineWidth = layout.ringLineWidth;
    ctx.arc(
      x,
      centerY,
      layout.fillRadius + layout.ringBaseOffset + index * layout.ringStep,
      0,
      Math.PI * 2
    );
    ctx.stroke();
  });

  ctx.restore();
}

function drawTimelineObject(
  ctx: CanvasRenderingContext2D,
  timelineObject: ObjectsTimelineObject,
  visualTheme: TimelineVisualTheme,
  startTimeMs: number,
  durationMs: number,
  width: number,
  centerY: number,
  visibleStartX: number,
  visibleEndX: number,
  isHitsoundView: boolean,
  neutralBodyColor: string,
  bodySample: ObjectsTimelineSample | null
) {
  const color = isHitsoundView ? neutralBodyColor : visualTheme.resolveObjectColor(timelineObject);
  const circleRadius = visualTheme.circleRadius(timelineObject);

  if (timelineObject.objectType === 'Circle') {
    const x = getTimelineX(timelineObject.startTimeMs, startTimeMs, durationMs, width);
    const outerRadius = isHitsoundView
      ? getHitsoundCircleOuterRadius(circleRadius, timelineObject.hitSoundFlags ?? 0)
      : circleRadius + 2;
    if (x < visibleStartX - outerRadius || x > visibleEndX + outerRadius) {
      return;
    }

    if (isHitsoundView) {
      drawHitsoundCircle(ctx, x, centerY, circleRadius, timelineObject.hitSoundFlags ?? 0);
      return;
    }

    drawThemedCircle(ctx, x, centerY, circleRadius, color, visualTheme.circle);
    return;
  }

  drawObjectBody(
    ctx,
    timelineObject,
    visualTheme,
    startTimeMs,
    durationMs,
    width,
    centerY,
    visibleStartX,
    visibleEndX,
    isHitsoundView,
    neutralBodyColor,
    bodySample
  );
}

function drawTimelineObjectMarkers(
  ctx: CanvasRenderingContext2D,
  timelineObject: ObjectsTimelineObject,
  visualTheme: TimelineVisualTheme,
  startTimeMs: number,
  durationMs: number,
  timelineWidth: number,
  centerY: number,
  visibleStartX: number,
  visibleEndX: number,
  isHitsoundView: boolean,
  neutralBodyColor: string,
  drawnMarkers: Set<string>
) {
  const { edges } = timelineObject;
  // Padded like the circle check in drawTimelineObject: a marker (slider head/tail, reverse
  // arrow, spinner) is drawn as a circle up to MARKER_CULL_PADDING_PX wide, so an unpadded check
  // here would let a marker near a tile boundary be attempted by only one tile — whose clip then
  // cuts off the half that belonged to the (never-drawn-by) neighboring tile.
  for (let edgeIndex = edges.length - 1; edgeIndex >= 0; edgeIndex -= 1) {
    const edge = edges[edgeIndex];
    const rawX = getTimelineX(edge.timeMs, startTimeMs, durationMs, timelineWidth);
    if (
      rawX < visibleStartX - MARKER_CULL_PADDING_PX ||
      rawX > visibleEndX + MARKER_CULL_PADDING_PX
    )
      continue;

    const x = getAlignedTimelineLineX(edge.timeMs, startTimeMs, durationMs, timelineWidth);
    const markerKey = `${timelineObject.objectType}-${edge.partName}-${x}`;
    if (drawnMarkers.has(markerKey)) continue;

    drawnMarkers.add(markerKey);
    drawObjectMarker(
      ctx,
      timelineObject,
      visualTheme,
      edge.partName,
      x,
      centerY,
      isHitsoundView,
      edge.hitSoundFlags ?? 0,
      neutralBodyColor
    );
  }
}

function drawSliderTickDots(
  ctx: CanvasRenderingContext2D,
  {
    samples,
    startTimeMs,
    durationMs,
    timelineWidth,
    centerY,
    visibleStartX,
    visibleEndX,
    startTimeFilterMs,
    endTimeFilterMs,
  }: {
    samples: ObjectsTimelineSample[];
    startTimeMs: number;
    durationMs: number;
    timelineWidth: number;
    centerY: number;
    visibleStartX: number;
    visibleEndX: number;
    startTimeFilterMs?: number;
    endTimeFilterMs?: number;
  }
) {
  if (samples.length === 0) {
    return;
  }

  const cullPadding = SLIDER_TICK_DOT_RADIUS + 1;

  // `samples` is sorted ascending by timeMs, so jump straight to the object's own time range
  // instead of scanning every sample in the map for every slider drawn.
  let startIndex = 0;
  let endIndexExclusive = samples.length;
  if (startTimeFilterMs != null && endTimeFilterMs != null) {
    startIndex = findLowerBoundIndex(samples, startTimeFilterMs - 1, (sample) => sample.timeMs);
    endIndexExclusive = findLowerBoundIndex(
      samples,
      endTimeFilterMs + 1 + 1e-6,
      (sample) => sample.timeMs
    );
  }

  ctx.save();
  ctx.fillStyle = withAlpha('#ffffff', SLIDER_TICK_DOT_ALPHA);

  for (let index = startIndex; index < endIndexExclusive; index += 1) {
    const sample = samples[index];
    if (sample.source !== 'Tick' || sample.objectType !== 'Slider') {
      continue;
    }

    const x = getTimelineX(sample.timeMs, startTimeMs, durationMs, timelineWidth);
    if (x < visibleStartX - cullPadding || x > visibleEndX + cullPadding) {
      continue;
    }

    ctx.beginPath();
    ctx.arc(x, centerY, SLIDER_TICK_DOT_RADIUS, 0, Math.PI * 2);
    ctx.fill();
  }

  ctx.restore();
}

function drawObjectBody(
  ctx: CanvasRenderingContext2D,
  timelineObject: ObjectsTimelineObject,
  visualTheme: TimelineVisualTheme,
  startTimeMs: number,
  durationMs: number,
  width: number,
  centerY: number,
  visibleStartX: number,
  visibleEndX: number,
  isHitsoundView: boolean,
  neutralBodyColor: string,
  bodySample: ObjectsTimelineSample | null
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

  const color = isHitsoundView
    ? timelineObject.objectType === 'Slider'
      ? withAlpha(
          getDominantHitsoundColor(timelineObject.sliderBodyHitSoundFlags ?? 0),
          SAMPLESET_BODY_ALPHA
        )
      : bodySample
        ? withAlpha(getSamplesetColor(bodySample.sampleset), SAMPLESET_BODY_ALPHA)
        : neutralBodyColor
    : visualTheme.resolveObjectColor(timelineObject);

  if (timelineObject.objectType === 'Spinner') {
    drawThemedLineBody(
      ctx,
      bodyBounds.startX,
      bodyBounds.endX,
      centerY,
      color,
      visualTheme.spinnerBody
    );
    return;
  }

  if (timelineObject.objectType === 'Hold note') {
    drawThemedHoldNote(
      ctx,
      bodyBounds.startX,
      bodyBounds.endX,
      centerY,
      color,
      visualTheme.holdNote
    );
    return;
  }

  drawThemedLineBody(
    ctx,
    bodyBounds.startX,
    bodyBounds.endX,
    centerY,
    color,
    visualTheme.sliderBody
  );
}

function drawObjectMarker(
  ctx: CanvasRenderingContext2D,
  timelineObject: ObjectsTimelineObject,
  visualTheme: TimelineVisualTheme,
  partName: string,
  x: number,
  centerY: number,
  isHitsoundView: boolean,
  edgeHitSoundFlags: number,
  neutralBodyColor: string
) {
  const lowerPart = partName.toLowerCase();
  const color = isHitsoundView ? neutralBodyColor : visualTheme.resolveObjectColor(timelineObject);
  const isSlider = timelineObject.objectType === 'Slider';
  const circleRadius = visualTheme.circleRadius(timelineObject);

  if (timelineObject.objectType === 'Spinner') {
    if (isHitsoundView) {
      drawHitsoundCircle(ctx, x, centerY, visualTheme.spinnerMarkerRadius, edgeHitSoundFlags);
      return;
    }

    drawThemedSpinnerMarker(
      ctx,
      x,
      centerY,
      color,
      visualTheme.spinnerMarkerRadius,
      visualTheme.spinnerMarker
    );
    return;
  }

  if (lowerPart.includes('reverse') || lowerPart.includes('repeat')) {
    if (isHitsoundView) {
      drawHitsoundCircle(ctx, x, centerY, circleRadius, edgeHitSoundFlags);
      drawThemedReverseArrow(ctx, x, centerY, REVERSE_ARROW_ICON_SIZE, visualTheme.reverseArrow);
      return;
    }

    drawThemedCircle(ctx, x, centerY, circleRadius, color, visualTheme.circle);
    drawThemedReverseArrow(ctx, x, centerY, REVERSE_ARROW_ICON_SIZE, visualTheme.reverseArrow);
    return;
  }

  if (lowerPart.includes('tail') || lowerPart.includes('end')) {
    if (isSlider) {
      if (isHitsoundView) {
        drawHitsoundCircle(ctx, x, centerY, circleRadius, edgeHitSoundFlags);
        return;
      }

      drawThemedSliderEndpoint(ctx, x, centerY, circleRadius, color, visualTheme.circle);
      return;
    }

    drawThemedObjectMarkerEdge(ctx, color, visualTheme.objectMarker);
    drawThemedTailSquare(ctx, x, centerY, visualTheme.objectMarker);
    return;
  }

  if (isHitsoundView) {
    drawHitsoundCircle(ctx, x, centerY, circleRadius, edgeHitSoundFlags);
    return;
  }

  if (isSlider) {
    drawThemedSliderEndpoint(ctx, x, centerY, circleRadius, color, visualTheme.circle);
    return;
  }

  drawThemedCircle(ctx, x, centerY, circleRadius, color, visualTheme.circle);
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
