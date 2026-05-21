import { getTimelineVisualTheme } from './registry.ts';
import type { TimelineThemeVariant, TimelineVisualTheme } from './types.ts';
import type { Mode } from '../../../../Types';

export type { TimelineThemeVariant } from './types.ts';

export const TIMELINE_THEME_VARIANT_OPTIONS: Array<{
  value: TimelineThemeVariant;
  label: string;
}> = [
  { value: 'default', label: 'Default' },
  { value: 'opaque', label: 'Opaque' },
];

export function resolveTimelineVisualTheme(
  difficultyMode: Mode | string,
  variant: TimelineThemeVariant
): TimelineVisualTheme {
  return getTimelineVisualTheme(difficultyMode, variant);
}

export function parseTimelineThemeVariant(value: string | null): TimelineThemeVariant {
  return value === 'opaque' ? 'opaque' : 'default';
}
