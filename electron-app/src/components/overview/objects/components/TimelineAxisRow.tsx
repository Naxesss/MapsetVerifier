import { Box, useMantineTheme } from '@mantine/core';
import { useRef, type MouseEvent as ReactMouseEvent } from 'react';
import { AXIS_HEIGHT, LABEL_WIDTH } from '../constants.ts';
import TimelineAxis from './TimelineAxis.tsx';
import { useTimelineEdges, useTimelineScale } from '../context/ObjectsTimelineContext.tsx';
import { getTimelineTimeFromX } from '../timelineUtils.ts';

interface TimelineAxisRowProps {
  linePosition: 'top' | 'bottom';
  onSeek?: (timeMs: number) => void;
}

const CLICK_MAX_DRAG_PX = 3;

export default function TimelineAxisRow({ linePosition, onSeek }: TimelineAxisRowProps) {
  const theme = useMantineTheme();
  const { startTimeMs, endTimeMs, timelineWidth, tickIntervalMs } = useTimelineScale();
  const { originX } = useTimelineEdges();
  const mouseDownXRef = useRef(0);

  const handleClick = (event: ReactMouseEvent<HTMLDivElement>) => {
    if (!onSeek || Math.abs(event.clientX - mouseDownXRef.current) > CLICK_MAX_DRAG_PX) {
      return;
    }

    const localX = event.clientX - event.currentTarget.getBoundingClientRect().left;
    const durationMs = Math.max(1, endTimeMs - startTimeMs);
    onSeek(getTimelineTimeFromX(localX, startTimeMs, durationMs, timelineWidth));
  };

  return (
    <Box
      style={{
        display: 'flex',
        alignItems: 'stretch',
        width: originX + timelineWidth,
        minWidth: originX + timelineWidth,
        height: AXIS_HEIGHT,
      }}
    >
      <Box
        style={{
          position: 'sticky',
          left: 0,
          zIndex: 3,
          flex: `0 0 ${LABEL_WIDTH}px`,
          height: AXIS_HEIGHT,
          background: theme.colors.dark[8],
          borderRight: `1px solid ${theme.colors.dark[4]}`,
          boxSizing: 'border-box',
        }}
      />
      <Box style={{ flex: `0 0 ${originX - LABEL_WIDTH}px` }} />
      <Box
        onMouseDown={(event) => {
          mouseDownXRef.current = event.clientX;
        }}
        onClick={handleClick}
        style={{
          flex: `0 0 ${timelineWidth}px`,
          minWidth: timelineWidth,
          width: timelineWidth,
          cursor: onSeek ? 'pointer' : undefined,
        }}
      >
        <TimelineAxis
          startTimeMs={startTimeMs}
          endTimeMs={endTimeMs}
          width={timelineWidth}
          tickIntervalMs={tickIntervalMs}
          linePosition={linePosition}
        />
      </Box>
    </Box>
  );
}
