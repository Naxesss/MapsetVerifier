import { Box, Stack, Text, Tooltip } from '@mantine/core';
import { formatEditorTimestamp, getSnapLabelColor, lookupEdgeSnapLabel } from '../timelineUtils.ts';
import type { ObjectsOverviewDifficulty } from '../../../../Types';
import type { TimelineObjectHeadHit } from '../timelineDrawing.ts';

type TimelineObjectHeadHovercardProps = {
  difficulty: ObjectsOverviewDifficulty;
  hover: TimelineObjectHeadHit | null;
  opened: boolean;
  anchorY: number;
};

export default function TimelineObjectHeadHovercard({
  difficulty,
  hover,
  opened,
  anchorY,
}: TimelineObjectHeadHovercardProps) {
  if (!hover) {
    return null;
  }

  const snapLabel = lookupEdgeSnapLabel(difficulty, hover.timeMs);
  const snapColor = getSnapLabelColor(snapLabel);

  return (
    <Tooltip
      position="top"
      withArrow
      arrowSize={10}
      opened={opened}
      openDelay={0}
      closeDelay={0}
      withinPortal
      multiline
      radius="md"
      events={{ hover: false, focus: false, touch: false }}
      label={
        <Stack gap={4} align="stretch" ta="left">
          <Text size="sm" fw={600} ta="left">
            {formatEditorTimestamp(hover.timeMs)}
          </Text>
          <Text size="xs" c="dimmed" ta="left">
            Part: {hover.partLabel}
          </Text>
          <Text size="xs" c="dimmed" ta="left">
            Snap:{' '}
            <Text span size="xs" fw={600} style={{ color: snapColor }}>
              {snapLabel}
            </Text>
          </Text>
        </Stack>
      }
      styles={{
        tooltip: {
          pointerEvents: 'none',
          '--tooltip-bg': 'var(--mantine-color-dark-6)',
          '--tooltip-color': 'var(--mantine-color-text)',
          backgroundColor: 'var(--mantine-color-dark-6)',
          color: 'var(--mantine-color-text)',
          border: '1px solid var(--mantine-color-dark-4)',
          boxShadow: 'var(--mantine-shadow-md)',
          padding: 'var(--mantine-spacing-xs) var(--mantine-spacing-sm)',
          textAlign: 'left',
        },
        arrow: {
          backgroundColor: 'var(--mantine-color-dark-6)',
          border: '1px solid var(--mantine-color-dark-4)',
        },
      }}
    >
      <Box
        style={{
          pointerEvents: 'none',
          position: 'absolute',
          left: hover.anchorX,
          top: anchorY,
          width: 1,
          height: 1,
          zIndex: 12,
        }}
      />
    </Tooltip>
  );
}
