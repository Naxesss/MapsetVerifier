import type { MantineTheme } from '@mantine/core';
import type { CSSProperties } from 'react';

const GROUP_COLOR_NAMES = ['yellow', 'cyan', 'grape', 'green', 'pink', 'lime', 'teal'] as const;

export function groupCellStyle(
  theme: MantineTheme,
  colorIndex: number | null
): CSSProperties | undefined {
  if (colorIndex === null) {
    return undefined;
  }

  const colorName = GROUP_COLOR_NAMES[colorIndex % GROUP_COLOR_NAMES.length];

  return {
    backgroundColor: `${theme.colors[colorName][9]}33`,
    boxShadow: `inset 3px 0 0 ${theme.colors[colorName][5]}`,
  };
}
