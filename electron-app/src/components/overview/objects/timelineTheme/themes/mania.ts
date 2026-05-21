import { mergeTimelineTheme } from '../mergeTheme.ts';
import { standardDefaultTheme, standardOpaqueTheme } from './standard.ts';

export const maniaDefaultTheme = mergeTimelineTheme(standardDefaultTheme, {
  mode: 'Mania',
});

export const maniaOpaqueTheme = mergeTimelineTheme(standardOpaqueTheme, {
  mode: 'Mania',
});
