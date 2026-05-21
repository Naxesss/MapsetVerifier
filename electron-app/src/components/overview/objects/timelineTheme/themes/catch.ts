import { mergeTimelineTheme } from '../mergeTheme.ts';
import { standardDefaultTheme, standardOpaqueTheme } from './standard.ts';

export const catchDefaultTheme = mergeTimelineTheme(standardDefaultTheme, {
  mode: 'Catch',
});

export const catchOpaqueTheme = mergeTimelineTheme(standardOpaqueTheme, {
  mode: 'Catch',
});
