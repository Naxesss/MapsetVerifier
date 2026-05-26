import { Badge, Transition } from '@mantine/core';

type TimelineShiftSeekModeBadgeProps = {
  visible: boolean;
};

export default function TimelineShiftSeekModeBadge({ visible }: TimelineShiftSeekModeBadgeProps) {
  return (
    <Transition mounted={visible} transition="slide-right" duration={150} timingFunction="ease">
      {(styles) => (
        <Badge
          color="green"
          variant="light"
          size="sm"
          style={{
            ...styles,
            position: 'absolute',
            top: 6,
            left: 28,
            zIndex: 25,
            pointerEvents: 'none',
          }}
        >
          Timeline scroll mode
        </Badge>
      )}
    </Transition>
  );
}
