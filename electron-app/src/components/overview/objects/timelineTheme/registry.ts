import { catchDefaultTheme, catchOpaqueTheme } from './themes/catch.ts';
import { maniaDefaultTheme, maniaOpaqueTheme } from './themes/mania.ts';
import { standardDefaultTheme, standardOpaqueTheme } from './themes/standard.ts';
import { taikoDefaultTheme, taikoOpaqueTheme } from './themes/taiko.ts';
import { normalizeMode } from '../../../../utils/gameMode';
import type { TimelineThemeVariant, TimelineVisualTheme } from './types.ts';
import type { Mode } from '../../../../Types';

const themes: Record<Mode, Record<TimelineThemeVariant, TimelineVisualTheme>> = {
  Standard: {
    default: standardDefaultTheme,
    opaque: standardOpaqueTheme,
  },
  Taiko: {
    default: taikoDefaultTheme,
    opaque: taikoOpaqueTheme,
  },
  Catch: {
    default: catchDefaultTheme,
    opaque: catchOpaqueTheme,
  },
  Mania: {
    default: maniaDefaultTheme,
    opaque: maniaOpaqueTheme,
  },
};

export function getTimelineVisualTheme(
  mode: Mode | string,
  variant: TimelineThemeVariant = 'default'
): TimelineVisualTheme {
  return themes[normalizeMode(mode)][variant];
}
