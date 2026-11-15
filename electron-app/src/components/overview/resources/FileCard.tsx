import { Box, SimpleGrid, Stack } from '@mantine/core';
import SectionCard from '../../common/SectionCard.tsx';
import type { ReactNode } from 'react';

interface FileCardProps {
  /** What the file is to look at or listen to: "Sound", "Video", "Image". */
  title: string;
  info?: ReactNode;
  /** Controls in the title row, e.g. opening the file elsewhere. */
  actions?: ReactNode;
  /** The file's key values as `StatField`s, three to a row. */
  stats: ReactNode;
  /** The file itself under its values: a waveform, a video, an image. */
  children?: ReactNode;
  /** Small print under the file, e.g. warnings from reading it. */
  footer?: ReactNode;
}

/**
 * The first card of every picked file on Resources, under the file's name: its key values, then
 * the file itself to see or hear. The same for the song, a video, a background and a hit sound.
 */
function FileCard({ title, info, actions, stats, children, footer }: FileCardProps) {
  return (
    <SectionCard title={title} info={info} actions={actions}>
      <Stack gap="sm">
        <SimpleGrid cols={{ base: 2, sm: 3 }} spacing="md">
          {stats}
        </SimpleGrid>
        {children}
        {footer}
      </Stack>
    </SectionCard>
  );
}

/**
 * A value in a file card that needs more room: two columns, e.g. the difficulties that use the
 * file, or the whole row with `full`.
 */
export function WideStat({ children, full = false }: { children: ReactNode; full?: boolean }) {
  return <Box style={{ gridColumn: full ? '1 / -1' : 'span 2' }}>{children}</Box>;
}

export default FileCard;
