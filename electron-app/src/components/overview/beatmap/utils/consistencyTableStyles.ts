import type { MantineTheme } from '@mantine/core';
import type { CSSProperties } from 'react';

const GROUP_COLOR_NAMES = ['yellow', 'cyan', 'grape', 'green', 'pink', 'lime', 'teal'] as const;

/**
 * A soft pill behind a value whose difficulties share it with others in the same group colour.
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

  return {
    display: 'inline-block',
    padding: '1px 7px',
    margin: '-1px -7px',
    borderRadius: 999,
    backgroundColor: `${theme.colors[colorName][9]}40`,
    color: theme.colors[colorName][3],
  };
}
