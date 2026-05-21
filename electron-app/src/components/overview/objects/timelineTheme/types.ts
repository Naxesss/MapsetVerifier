import type { Mode, ObjectsTimelineObject } from '../../../../Types';

export type TimelineThemeVariant = 'default' | 'opaque';

export type ColorRef = { kind: 'object' } | { kind: 'fixed'; color: string };

export type AlphaFill = {
  color: ColorRef;
  alpha: number;
};

export type AlphaStroke = {
  color: ColorRef;
  alpha: number;
  width: number;
  lineCap?: CanvasLineCap;
};

export type TimelineCircleTheme = {
  fill: AlphaFill;
  border: AlphaStroke;
};

export type TimelineLineBodyTheme = {
  glow: AlphaStroke;
  core: AlphaStroke;
};

export type TimelineHoldNoteTheme = {
  fill: AlphaFill;
  stroke: AlphaStroke;
  height: number;
};

export type TimelineSpinnerMarkerTheme = {
  ringFill: AlphaFill;
  ringStroke: AlphaStroke;
  ringStrokeWidthScale: number;
  ringStrokeWidthMin: number;
  ringRadiusScale: number;
  ringRadiusInset: number;
  arcStroke: string;
  arcStrokeWidthScale: number;
  arcStrokeWidthMin: number;
  arcStartAngle: number;
  arcEndAngle: number;
  accentFill: string;
  accentRadiusScale: number;
  accentRadiusMin: number;
  accentOffsetXScale: number;
  accentOffsetYScale: number;
  centerFill: AlphaFill;
  centerRadiusScale: number;
  centerRadiusMin: number;
};

export type TimelineObjectMarkerTheme = {
  edgeStroke: AlphaStroke;
  tailSquareSize: number;
};

export type TimelineReverseArrowTheme = {
  stroke: string;
  lineWidthScale: number;
  lineWidthMin: number;
};

export type TimelineVisualTheme = {
  mode: Mode;
  resolveObjectColor: (object: ObjectsTimelineObject) => string;
  circleRadius: (object: ObjectsTimelineObject) => number;
  spinnerMarkerRadius: number;
  circle: TimelineCircleTheme;
  sliderBody: TimelineLineBodyTheme;
  spinnerBody: TimelineLineBodyTheme;
  holdNote: TimelineHoldNoteTheme;
  spinnerMarker: TimelineSpinnerMarkerTheme;
  objectMarker: TimelineObjectMarkerTheme;
  reverseArrow: TimelineReverseArrowTheme;
};

export type TimelineVisualThemeOverride = Partial<{
  mode: Mode;
  resolveObjectColor: (object: ObjectsTimelineObject) => string;
  circleRadius: (object: ObjectsTimelineObject) => number;
  spinnerMarkerRadius: number;
  circle: Partial<TimelineCircleTheme>;
  sliderBody: Partial<TimelineLineBodyTheme>;
  spinnerBody: Partial<TimelineLineBodyTheme>;
  holdNote: Partial<TimelineHoldNoteTheme>;
  spinnerMarker: Partial<TimelineSpinnerMarkerTheme>;
  objectMarker: Partial<TimelineObjectMarkerTheme>;
  reverseArrow: Partial<TimelineReverseArrowTheme>;
}>;
