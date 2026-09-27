import { Badge, Tooltip } from '@mantine/core';
import ShortcutLabel from '../../../common/ShortcutLabel.tsx';

type TimelineZoomModeBadgeProps = {
  active: boolean;
};

export default function TimelineZoomModeBadge({ active }: TimelineZoomModeBadgeProps) {
  return (
    <Tooltip label={<ShortcutLabel label="Zoom the timeline" keys={['Ctrl', 'Scroll']} />}>
      <Badge
        color={active ? 'blue' : 'gray'}
        style={{ opacity: active ? 1 : 0.45, cursor: 'default' }}
      >
        Timeline zoom
      </Badge>
    </Tooltip>
  );
}
