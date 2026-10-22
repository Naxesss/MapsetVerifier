import { Group, Kbd, SegmentedControl, Text } from '@mantine/core';
import { TIMELINE_SCROLL_TICK_STEP_OPTIONS, type TimelineScrollTickStep } from '../constants.ts';
import type { ReactNode } from 'react';

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

interface TimelineShortcutHintsProps {
  zoomActive: boolean;
  scrollActive: boolean;
  tickStep: TimelineScrollTickStep;
  onTickStepChange: (value: TimelineScrollTickStep) => void;
}

/** One shortcut, dimmed at rest and in the primary colour while its key is held. */
function Hint({ active, keys, children }: { active: boolean; keys: string; children: ReactNode }) {
  return (
    <Text
      size="xs"
      c={active ? 'primary.2' : 'dimmed'}
      fw={active ? 700 : undefined}
      style={{ whiteSpace: 'nowrap', transition: 'color 0.12s ease' }}
    >
      <Kbd size="xs">{keys}</Kbd> + scroll {children}
    </Text>
  );
}

/**
 * The timeline's scroll shortcuts as a line of text rather than buttons, with the one whose key is
 * held lit up. The ticks-per-step picker sits in the Shift hint, where it applies.
 */
export default function TimelineShortcutHints({
  zoomActive,
  scrollActive,
  tickStep,
  onTickStepChange,
}: TimelineShortcutHintsProps) {
  return (
    <Group
      gap="md"
      wrap="wrap"
      align="center"
      data-stop-timeline-pan="true"
      data-timeline-wheel-ignore="true"
    >
      <Hint active={zoomActive} keys="Ctrl">
        zooms
      </Hint>
      <Group gap="xs" wrap="nowrap" align="center">
        <Hint active={scrollActive} keys="Shift">
          steps
        </Hint>
        <SegmentedControl
          aria-label="Timing ticks per scroll step"
          size="xs"
          value={String(tickStep)}
          onChange={(value) => onTickStepChange(Number(value) as TimelineScrollTickStep)}
          styles={hideFocusRingStyles}
          data={TIMELINE_SCROLL_TICK_STEP_OPTIONS.map((step) => ({
            label: String(step),
            value: String(step),
          }))}
        />
        <Text size="xs" c={scrollActive ? 'primary.2' : 'dimmed'} style={{ whiteSpace: 'nowrap' }}>
          ticks
        </Text>
      </Group>
    </Group>
  );
}
