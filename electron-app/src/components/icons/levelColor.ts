import type { Level } from '../../Types';
import type { MantineTheme } from '@mantine/core';

/** The colour of each level's icon (see LevelIcon), for places that show a level as colour only. */
export function levelColor(level: Level, theme: MantineTheme): string {
  switch (level) {
    case 'Problem':
      return theme.colors.red[6];
    case 'Warning':
      return theme.colors.orange[6];
    case 'Info':
      return theme.colors.teal[6];
    case 'Error':
      return theme.colors.gray[5];
    case 'Minor':
    case 'Check':
    default:
      return theme.colors.green[6];
  }
}
