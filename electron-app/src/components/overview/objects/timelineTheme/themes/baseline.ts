import type { TimelineVisualTheme } from '../types.ts';

const objectColorRef = { kind: 'object' as const };

/** Shared draw tokens — verbatim from pre-refactor timelineDrawing.ts */
export const baselineTimelineThemeTokens = {
  circle: {
    fill: { color: objectColorRef, alpha: 0.55 },
    border: { color: objectColorRef, alpha: 0.98, width: 1.75 },
  },
  sliderBody: {
    glow: { color: objectColorRef, alpha: 0.4, width: 32, lineCap: 'round' as CanvasLineCap },
    core: { color: objectColorRef, alpha: 0.95, width: 2 },
  },
  spinnerBody: {
    glow: { color: objectColorRef, alpha: 0.18, width: 32, lineCap: 'round' as CanvasLineCap },
    core: { color: objectColorRef, alpha: 0.45, width: 4 },
  },
  holdNote: {
    fill: { color: objectColorRef, alpha: 0.42 },
    stroke: { color: objectColorRef, alpha: 0.85, width: 1.5 },
    height: 10,
  },
  spinnerMarker: {
    ringFill: { color: objectColorRef, alpha: 0.22 },
    ringStroke: { color: objectColorRef, alpha: 0.9, width: 0 },
    ringStrokeWidthScale: 0.1,
    ringStrokeWidthMin: 1.25,
    ringRadiusScale: 0.72,
    ringRadiusInset: 4,
    arcStroke: 'rgba(255,255,255,0.95)',
    arcStrokeWidthScale: 0.16,
    arcStrokeWidthMin: 1.75,
    arcStartAngle: -Math.PI * 0.35,
    arcEndAngle: Math.PI * 1.15,
    accentFill: 'rgba(255,255,255,0.95)',
    accentRadiusScale: 0.12,
    accentRadiusMin: 1.4,
    accentOffsetXScale: 0.55,
    accentOffsetYScale: 0.6,
    centerFill: { color: objectColorRef, alpha: 0.92 },
    centerRadiusScale: 0.15,
    centerRadiusMin: 1.6,
  },
  objectMarker: {
    edgeStroke: { color: objectColorRef, alpha: 0.95, width: 1.5 },
    tailSquareSize: 6,
  },
  reverseArrow: {
    stroke: 'rgba(255,255,255,0.95)',
    lineWidthScale: 0.36,
    lineWidthMin: 1.4,
  },
};

/** Placeholder resolvers — replaced by per-mode themes via mergeTimelineTheme */
export const baselineTimelineTheme: TimelineVisualTheme = {
  mode: 'Standard',
  resolveObjectColor: () => '#ced4da',
  circleRadius: () => 12,
  spinnerMarkerRadius: 14.5,
  ...baselineTimelineThemeTokens,
};
