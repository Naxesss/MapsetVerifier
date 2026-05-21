import type {
  TimelineCircleTheme,
  TimelineHoldNoteTheme,
  TimelineLineBodyTheme,
  TimelineObjectMarkerTheme,
  TimelineReverseArrowTheme,
  TimelineSpinnerMarkerTheme,
  TimelineVisualTheme,
  TimelineVisualThemeOverride,
} from './types.ts';

function mergeCircleTheme(
  base: TimelineCircleTheme,
  partial?: Partial<TimelineCircleTheme>
): TimelineCircleTheme {
  if (!partial) return base;
  return {
    fill: { ...base.fill, ...partial.fill },
    border: { ...base.border, ...partial.border },
  };
}

function mergeLineBodyTheme(
  base: TimelineLineBodyTheme,
  partial?: Partial<TimelineLineBodyTheme>
): TimelineLineBodyTheme {
  if (!partial) return base;
  return {
    glow: { ...base.glow, ...partial.glow },
    core: { ...base.core, ...partial.core },
  };
}

function mergeHoldNoteTheme(
  base: TimelineHoldNoteTheme,
  partial?: Partial<TimelineHoldNoteTheme>
): TimelineHoldNoteTheme {
  if (!partial) return base;
  return {
    ...base,
    fill: { ...base.fill, ...partial.fill },
    stroke: { ...base.stroke, ...partial.stroke },
  };
}

function mergeSpinnerMarkerTheme(
  base: TimelineSpinnerMarkerTheme,
  partial?: Partial<TimelineSpinnerMarkerTheme>
): TimelineSpinnerMarkerTheme {
  if (!partial) return base;
  return { ...base, ...partial };
}

function mergeObjectMarkerTheme(
  base: TimelineObjectMarkerTheme,
  partial?: Partial<TimelineObjectMarkerTheme>
): TimelineObjectMarkerTheme {
  if (!partial) return base;
  return {
    ...base,
    edgeStroke: { ...base.edgeStroke, ...partial.edgeStroke },
  };
}

function mergeReverseArrowTheme(
  base: TimelineReverseArrowTheme,
  partial?: Partial<TimelineReverseArrowTheme>
): TimelineReverseArrowTheme {
  if (!partial) return base;
  return { ...base, ...partial };
}

export function mergeTimelineTheme(
  base: TimelineVisualTheme,
  partial: TimelineVisualThemeOverride
): TimelineVisualTheme {
  return {
    mode: partial.mode ?? base.mode,
    resolveObjectColor: partial.resolveObjectColor ?? base.resolveObjectColor,
    circleRadius: partial.circleRadius ?? base.circleRadius,
    spinnerMarkerRadius: partial.spinnerMarkerRadius ?? base.spinnerMarkerRadius,
    circle: mergeCircleTheme(base.circle, partial.circle),
    sliderBody: mergeLineBodyTheme(base.sliderBody, partial.sliderBody),
    spinnerBody: mergeLineBodyTheme(base.spinnerBody, partial.spinnerBody),
    holdNote: mergeHoldNoteTheme(base.holdNote, partial.holdNote),
    spinnerMarker: mergeSpinnerMarkerTheme(base.spinnerMarker, partial.spinnerMarker),
    objectMarker: mergeObjectMarkerTheme(base.objectMarker, partial.objectMarker),
    reverseArrow: mergeReverseArrowTheme(base.reverseArrow, partial.reverseArrow),
  };
}
