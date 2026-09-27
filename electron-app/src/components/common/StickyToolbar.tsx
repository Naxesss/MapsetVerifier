import { Stack } from '@mantine/core';
import type { ReactNode } from 'react';

/**
 * Search and filter rows that stay in view while a long list scrolls below them (Documentation,
 * Ranking criteria). Padded and spaced like the beatmap sidebar's search row so both line up once
 * it sticks; negative margins keep the page spacing unchanged.
 */
export default function StickyToolbar({ children }: { children: ReactNode }) {
  return (
    <Stack
      gap="sm"
      py="xs"
      style={{
        position: 'sticky',
        top: 0,
        zIndex: 2,
        margin: 'calc(var(--mantine-spacing-xs) * -1) 0',
        background: 'var(--mantine-color-body)',
      }}
    >
      {children}
    </Stack>
  );
}
