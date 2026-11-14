import { TOP_BAR_HEIGHT } from './layers.ts';
import type { CSSVariablesResolver } from '@mantine/core';

export const cssVarResolver: CSSVariablesResolver = () => ({
  variables: {
    // Height of the bar at the top of the window, for the CSS that has to stay below it.
    '--mv-top-bar-height': `${TOP_BAR_HEIGHT}px`,
  },
  light: {},
  dark: {
    // Default dark mode makes the text color use --mantine-color-dark-0 which we don't want
    '--mantine-color-text': '#fff',
    '--mantine-color-dimmed': '#9e9e9e',
  },
});
