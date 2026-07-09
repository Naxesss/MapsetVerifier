import { Badge, Tooltip } from '@mantine/core';

type TimelineZoomModeBadgeProps = {
  active: boolean;
};

export default function TimelineZoomModeBadge({ active }: TimelineZoomModeBadgeProps) {
  return (
    <Tooltip label="Hold Ctrl and scroll the timeline to zoom" withArrow>
      <Badge
        color={active ? 'blue' : 'gray'}
        variant="light"
        size="sm"
        style={{ opacity: active ? 1 : 0.45, cursor: 'default' }}
      >
        Timeline zoom
      </Badge>
    </Tooltip>
  );
}
