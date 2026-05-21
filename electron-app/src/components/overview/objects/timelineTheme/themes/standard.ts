import { CIRCLE_OBJECT_RADIUS, TAIKO_CIRCLE_RADIUS } from '../../constants.ts';
import { mergeTimelineTheme } from '../mergeTheme.ts';
import { baselineTimelineTheme } from './baseline.ts';
import { opaqueCircleTheme } from './opaqueCircle.ts';
import type { ObjectsTimelineObject } from '../../../../../Types';

function resolveStandardObjectColor(object: ObjectsTimelineObject): string {
  return object.comboColourHex ?? '#ced4da';
}

const standardResolvers = {
  mode: 'Standard' as const,
  resolveObjectColor: resolveStandardObjectColor,
  circleRadius: () => TAIKO_CIRCLE_RADIUS,
  spinnerMarkerRadius: CIRCLE_OBJECT_RADIUS - 1.5,
};

export const standardDefaultTheme = mergeTimelineTheme(baselineTimelineTheme, standardResolvers);

export const standardOpaqueTheme = mergeTimelineTheme(standardDefaultTheme, {
  circle: opaqueCircleTheme,
});
