import { Box, Stack } from '@mantine/core';
import { ReactNode } from 'react';
import { useMapsetHeaderRef } from './mapsetFrameContext.ts';

interface BeatmapHeaderProps {
  children?: ReactNode;
}

/**
 * A mapset page's controls, shown on the banner art right under the mapset title. The art, title
 * and mapper belong to the surrounding MapsetFrame, which stays put while switching pages; the art
 * window ends where this header ends. No bottom padding: the content below adds the one gap
 * between the header's last row and the page.
 */
function BeatmapHeader({ children }: BeatmapHeaderProps) {
  const registerHeader = useMapsetHeaderRef();

  return (
    <Box ref={registerHeader} pt="sm" px="md">
      <Stack gap="sm">{children}</Stack>
    </Box>
  );
}

export default BeatmapHeader;
