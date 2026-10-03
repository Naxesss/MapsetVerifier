import type { Level } from '../../Types';
import type { MantineTheme } from '@mantine/core';

/** The Mantine colour of each level, the same as its icon (see LevelIcon), e.g. for badges. */
export function levelColorName(level: Level): 'red' | 'orange' | 'teal' | 'gray' | 'green' {
  switch (level) {
    case 'Problem':
      return 'red';
    case 'Warning':
      return 'orange';
    case 'Info':
      return 'teal';
    case 'Error':
      return 'gray';
    case 'Minor':
    case 'Check':
    default:
      return 'green';
  }
}

/** The colour of each level's icon (see LevelIcon), for places that show a level as colour only. */
export function levelColor(level: Level, theme: MantineTheme): string {
  const name = levelColorName(level);
  return theme.colors[name][name === 'gray' ? 5 : 6];
}
