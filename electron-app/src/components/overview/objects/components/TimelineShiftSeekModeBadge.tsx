import { Badge, Group, SegmentedControl, Tooltip } from '@mantine/core';
import { TIMELINE_SCROLL_TICK_STEP_OPTIONS, type TimelineScrollTickStep } from '../constants.ts';

/** Mantine draws the focus ring on the label via --segmented-control-outline when the radio is focused. */
const hideFocusRingStyles = {
  label: {
    outline: 'none',
    '--segmented-control-outline': 'none',
  },
  input: {
    '&:focus + label': {
      '--segmented-control-outline': 'none',
      outline: 'none',
    },
    '&:focus-visible + label': {
      '--segmented-control-outline': 'none',
      outline: 'none',
    },
  },
} as const;

type TimelineShiftSeekModeBadgeProps = {
  active: boolean;
  tickStep: TimelineScrollTickStep;
  onTickStepChange: (value: TimelineScrollTickStep) => void;
};

export default function TimelineShiftSeekModeBadge({
  active,
  tickStep,
  onTickStepChange,
}: TimelineShiftSeekModeBadgeProps) {
  return (
    <Group
      gap={6}
      wrap="nowrap"
      align="center"
      data-stop-timeline-pan="true"
      data-timeline-wheel-ignore="true"
    >
      <Tooltip label="Hold Shift and scroll the timeline" withArrow>
        <Badge
          color={active ? 'green' : 'gray'}
          variant="light"
          size="sm"
          style={{ opacity: active ? 1 : 0.45, cursor: 'default' }}
        >
          Timeline scroll
        </Badge>
      </Tooltip>
      <Tooltip label="Timing ticks per scroll step" withArrow>
        <SegmentedControl
          aria-label="Timeline scroll tick step"
          size="xs"
          value={String(tickStep)}
          onChange={(value) => onTickStepChange(Number(value) as TimelineScrollTickStep)}
          styles={hideFocusRingStyles}
          data={TIMELINE_SCROLL_TICK_STEP_OPTIONS.map((step) => ({
            label: String(step),
            value: String(step),
          }))}
        />
      </Tooltip>
    </Group>
  );
}
