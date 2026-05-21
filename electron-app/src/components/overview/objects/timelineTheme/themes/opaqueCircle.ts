import type { TimelineCircleTheme } from '../types.ts';

/** Opaque fill with white border — shared across modes when "Opaque" variant is selected. */
export const opaqueCircleTheme: TimelineCircleTheme = {
  fill: { color: { kind: 'object' }, alpha: 1 },
  border: { color: { kind: 'fixed', color: '#ffffff' }, alpha: 1, width: 1.35 },
};
