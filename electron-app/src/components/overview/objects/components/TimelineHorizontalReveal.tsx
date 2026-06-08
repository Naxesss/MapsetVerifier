import { Box } from '@mantine/core';
import {
  TIMELINE_VIEW_MODE_TRANSITION_EASING,
  TIMELINE_VIEW_MODE_TRANSITION_MS,
} from '../constants.ts';
import type { ReactNode } from 'react';

type TimelineHorizontalRevealProps = {
  visible: boolean;
  children: ReactNode;
  /** Space before content when visible; collapses to 0 so flex siblings slide smoothly. */
  spacing?: number | string;
};

const transition = `${TIMELINE_VIEW_MODE_TRANSITION_MS}ms ${TIMELINE_VIEW_MODE_TRANSITION_EASING}`;

export default function TimelineHorizontalReveal({
  visible,
  children,
  spacing = 0,
}: TimelineHorizontalRevealProps) {
  return (
    <Box
      style={{
        display: 'grid',
        gridTemplateColumns: visible ? '1fr' : '0fr',
        marginLeft: visible ? spacing : 0,
        opacity: visible ? 1 : 0,
        overflow: 'hidden',
        pointerEvents: visible ? undefined : 'none',
        transition: [
          `grid-template-columns ${transition}`,
          `margin-left ${transition}`,
          `opacity ${transition}`,
        ].join(', '),
      }}
    >
      <Box style={{ minWidth: 0, overflow: 'hidden' }}>{children}</Box>
    </Box>
  );
}
