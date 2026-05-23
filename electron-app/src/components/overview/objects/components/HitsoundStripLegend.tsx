import { Box, Group, Stack, Text } from '@mantine/core';
import { HITSOUND_COLORS, SAMPLESET_TINTS } from '../hitsoundUtils.ts';
import {
  BODY_MARKER_HEIGHT,
  BODY_MARKER_WIDTH,
  SOUND_STRIP_EDGE_LANE_HEIGHT,
  SOUND_STRIP_LANE_GAP,
  SOUND_STRIP_PASSIVE_LANE_HEIGHT,
  TICK_MARKER_RADIUS,
} from '../timelineHitsoundDrawing.ts';
import type { ReactNode } from 'react';

const EDGE_LEGEND_DOT_RADIUS = 4;

function EdgeMarkerPreview({ color }: { color: string }) {
  return (
    <Box
      style={{
        width: EDGE_LEGEND_DOT_RADIUS * 2,
        height: EDGE_LEGEND_DOT_RADIUS * 2,
        borderRadius: '50%',
        background: color,
        border: '1px solid rgba(255, 255, 255, 0.45)',
        boxSizing: 'border-box',
      }}
    />
  );
}

function BodyMarkerPreview() {
  return (
    <Box
      style={{
        width: BODY_MARKER_WIDTH,
        height: BODY_MARKER_HEIGHT,
        background: HITSOUND_COLORS.body,
      }}
    />
  );
}

function TickMarkerPreview() {
  return (
    <Box
      style={{
        width: TICK_MARKER_RADIUS * 2,
        height: TICK_MARKER_RADIUS * 2,
        borderRadius: '50%',
        background: HITSOUND_COLORS.tick,
        border: '1px solid rgba(255, 255, 255, 0.25)',
        boxSizing: 'border-box',
      }}
    />
  );
}

function SamplesetPreview({ color, label }: { color: string; label: string }) {
  return (
    <Group gap={6} wrap="nowrap">
      <Box
        style={{
          width: 28,
          height: 8,
          borderRadius: 2,
          background: color,
          borderTop: `2px solid ${color}`,
          opacity: 0.85,
        }}
      />
      <Text size="xs" c="dimmed">
        {label}
      </Text>
    </Group>
  );
}

function LanePreview({
  label,
  height,
  children,
}: {
  label: string;
  height: number;
  children: ReactNode;
}) {
  return (
    <Group gap={8} wrap="nowrap" align="center">
      <Text size="xs" c="dimmed" w={92} style={{ flexShrink: 0 }}>
        {label}
      </Text>
      <Box
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 8,
          minHeight: height,
          paddingInline: 8,
          borderRadius: 4,
          border: '1px solid rgba(255, 255, 255, 0.08)',
        }}
      >
        {children}
      </Box>
    </Group>
  );
}

export default function HitsoundStripLegend() {
  const edgeHitsounds = [
    { label: 'Normal', color: HITSOUND_COLORS.normal },
    { label: 'Whistle', color: HITSOUND_COLORS.whistle },
    { label: 'Clap', color: HITSOUND_COLORS.clap },
    { label: 'Finish', color: HITSOUND_COLORS.finish },
  ] as const;

  return (
    <Stack gap={6}>
      <LanePreview label="Edge hitsounds" height={SOUND_STRIP_EDGE_LANE_HEIGHT}>
        {edgeHitsounds.map((item) => (
          <Group key={item.label} gap={6} wrap="nowrap">
            <EdgeMarkerPreview color={item.color} />
            <Text size="xs" c="dimmed">
              {item.label}
            </Text>
          </Group>
        ))}
      </LanePreview>

      <LanePreview
        label="Passive samples"
        height={SOUND_STRIP_PASSIVE_LANE_HEIGHT + SOUND_STRIP_LANE_GAP}
      >
        <Group gap={6} wrap="nowrap">
          <BodyMarkerPreview />
          <Text size="xs" c="dimmed">
            Body
          </Text>
        </Group>
        <Group gap={6} wrap="nowrap">
          <TickMarkerPreview />
          <Text size="xs" c="dimmed">
            Tick
          </Text>
        </Group>
      </LanePreview>

      <Group gap={8} wrap="nowrap" align="flex-start">
        <Text size="xs" c="dimmed" w={92} style={{ flexShrink: 0, paddingTop: 2 }}>
          Sample bank
        </Text>
        <Stack gap={4}>
          <Group gap="md" wrap="wrap">
            <SamplesetPreview color={SAMPLESET_TINTS.Soft} label="Soft section" />
            <SamplesetPreview color={SAMPLESET_TINTS.Drum} label="Drum section" />
          </Group>
        </Stack>
      </Group>
    </Stack>
  );
}
