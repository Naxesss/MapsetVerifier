import {
  TAIKO_CIRCLE_RADIUS,
  TAIKO_FINISHER_CIRCLE_RADIUS,
  TAIKO_SPINNER_RADIUS,
} from '../../constants.ts';
import { mergeTimelineTheme } from '../mergeTheme.ts';
import { baselineTimelineTheme } from './baseline.ts';
import { opaqueCircleTheme } from './opaqueCircle.ts';
import type { ObjectsTimelineObject } from '../../../../../Types';

export const TAIKO_DRUMROLL_COLOR = 'rgb(252,191,31)';
export const TAIKO_SPINNER_COLOR = 'rgb(125,135,150)';

function resolveTaikoObjectColor(object: ObjectsTimelineObject): string {
  if (object.objectType === 'Spinner') {
    return TAIKO_SPINNER_COLOR;
  }

  if (object.objectType === 'Slider') {
    return TAIKO_DRUMROLL_COLOR;
  }

  return object.comboColourHex ?? '#ced4da';
}

const taikoResolvers = {
  mode: 'Taiko' as const,
  resolveObjectColor: resolveTaikoObjectColor,
  circleRadius: (object: ObjectsTimelineObject) =>
    object.hasFinishHitSound ? TAIKO_FINISHER_CIRCLE_RADIUS : TAIKO_CIRCLE_RADIUS,
  spinnerMarkerRadius: TAIKO_SPINNER_RADIUS,
};

/** Translucent circles + taiko colors/radii (baseline circle tokens). */
export const taikoDefaultTheme = mergeTimelineTheme(baselineTimelineTheme, taikoResolvers);

/** Opaque circles with white border + taiko colors/radii. */
export const taikoOpaqueTheme = mergeTimelineTheme(taikoDefaultTheme, {
  circle: opaqueCircleTheme,
});
