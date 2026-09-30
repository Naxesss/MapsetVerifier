import type { MantineTheme } from '@mantine/core';
import type { CSSProperties } from 'react';

const GROUP_COLOR_NAMES = ['yellow', 'cyan', 'grape', 'green', 'pink', 'lime', 'teal'] as const;

/**
 * Text colour for a value that shares its group colour with other difficulties.
 * The dominant value has no colour, so only the values that differ stand out.
 */
export function groupValueStyle(
  theme: MantineTheme,
  colorIndex: number | null
): CSSProperties | undefined {
  if (colorIndex === null) {
    return undefined;
  }

  const colorName = GROUP_COLOR_NAMES[colorIndex % GROUP_COLOR_NAMES.length];

  return { color: theme.colors[colorName][3] };
}

/** The uncoloured value is the majority; each other colour is the next group. */
export function groupValueLabel(colorIndex: number | null): string {
  return colorIndex === null ? 'Majority group' : `Group ${colorIndex + 1}`;
}
