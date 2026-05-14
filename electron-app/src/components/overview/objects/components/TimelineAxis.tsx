import { Box, useMantineTheme } from '@mantine/core';
import { AXIS_HEIGHT } from '../constants.ts';
import { formatTime, getAlignedTimelineLineX } from '../timelineUtils.ts';

interface TimelineAxisProps {
  startTimeMs: number;
  endTimeMs: number;
  width: number;
  tickIntervalMs: number;
  linePosition: 'top' | 'bottom';
}

export default function TimelineAxis({
  startTimeMs,
  endTimeMs,
  width,
  tickIntervalMs,
  linePosition,
}: TimelineAxisProps) {
  const theme = useMantineTheme();
  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const firstTickMs = Math.ceil(startTimeMs / tickIntervalMs) * tickIntervalMs;
  const ticks: number[] = [];
  const lineY = linePosition === 'bottom' ? AXIS_HEIGHT - 1.5 : 1.5;
  const tickEndY = linePosition === 'bottom' ? lineY - 8 : lineY + 8;
  const labelY = linePosition === 'bottom' ? 11 : AXIS_HEIGHT - 9;

  for (let tick = firstTickMs; tick <= endTimeMs; tick += tickIntervalMs) {
    ticks.push(tick);
  }

  return (
    <Box style={{ position: 'relative', width, height: AXIS_HEIGHT }}>
      <svg width={width} height={AXIS_HEIGHT} style={{ display: 'block' }}>
        <line
          x1={0}
          y1={lineY}
          x2={width}
          y2={lineY}
          stroke={theme.colors.dark[4]}
          strokeWidth={1}
        />
        {ticks.map((tick) => {
          const x = getAlignedTimelineLineX(tick, startTimeMs, durationMs, width);
          return (
            <g key={tick}>
              <line
                x1={x}
                y1={lineY}
                x2={x}
                y2={tickEndY}
                stroke={theme.colors.dark[3]}
                strokeWidth={1}
              />
              <text
                x={x}
                y={labelY}
                fill={theme.colors.dark[2]}
                fontSize="10"
                textAnchor="middle"
                dominantBaseline="middle"
                fontFamily={theme.fontFamily}
              >
                {formatTime(tick)}
              </text>
            </g>
          );
        })}
      </svg>
    </Box>
  );
}
