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
        // `grid-template-columns: 0fr` only collapses width — the grid row still sizes to the
        // child's natural content height regardless, so a hidden reveal was still reserving its
        // full height and inflating the flex row it sits in. Collapse height directly too; it
        // isn't transitioned (CSS can't animate to/from `auto`), but since it only matters while
        // width/opacity are already at (or animating through) zero, the snap isn't visible.
        height: visible ? 'auto' : 0,
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
