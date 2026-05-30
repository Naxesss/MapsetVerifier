import { Badge, Box, Tooltip } from '@mantine/core';

type TimelineShiftSeekModeBadgeProps = {
  active: boolean;
};

export default function TimelineShiftSeekModeBadge({ active }: TimelineShiftSeekModeBadgeProps) {
  return (
    <Tooltip label="Hold Shift and scroll the timeline" withArrow>
      <Box
        style={{
          position: 'absolute',
          top: 6,
          left: 28,
          zIndex: 25,
          display: 'inline-flex',
        }}
      >
        <Badge
          color={active ? 'green' : 'gray'}
          variant="light"
          size="sm"
          style={{ opacity: active ? 1 : 0.45 }}
        >
          Timeline scroll mode
        </Badge>
      </Box>
    </Tooltip>
  );
}
