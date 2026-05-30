import { Box, Group, Stack, Text } from '@mantine/core';
import { withAlpha } from '../../../../utils/color.ts';
import {
  EDITOR_SAMPLE_BANK_COLORS,
  HITSOUND_COLORS,
  HITSOUND_GAP_OVERLAY_ALPHA,
  HITSOUND_GAP_OVERLAY_COLOR,
  SAMPLESET_ACCENT_ALPHA,
  SAMPLESET_BODY_ALPHA,
  SAMPLESET_OVERLAY_ALPHA,
} from '../hitsoundUtils.ts';
import {
  BODY_MARKER_HEIGHT,
  BODY_MARKER_WIDTH,
  EDGE_MARKER_WIDTH,
  SOUND_STRIP_EDGE_LANE_HEIGHT,
  SOUND_STRIP_LANE_GAP,
  SOUND_STRIP_PASSIVE_LANE_HEIGHT,
  TICK_MARKER_RADIUS,
} from '../timelineHitsoundDrawing.ts';
import type { ReactNode } from 'react';

function EdgeMarkerPreview({ color }: { color: string }) {
  return (
    <Box
      style={{
        width: EDGE_MARKER_WIDTH,
        height: SOUND_STRIP_EDGE_LANE_HEIGHT - 4,
        background: color,
        border: '1px solid rgba(255, 255, 255, 0.45)',
        boxSizing: 'border-box',
      }}
    />
  );
}

function BodyMarkerPreview({
  color,
  additionColor,
  scale = 1,
}: {
  color: string;
  additionColor?: string;
  scale?: number;
}) {
  const width = BODY_MARKER_WIDTH * scale;
  const height = BODY_MARKER_HEIGHT * scale;
  const borderWidth = scale >= 2 ? 2 : 1.5;

  return (
    <Box
      style={{
        width,
        height,
        background: color,
        boxSizing: 'border-box',
        border: additionColor ? `${borderWidth}px solid ${additionColor}` : undefined,
        flexShrink: 0,
      }}
    />
  );
}

function TickMarkerPreview({ color, scale = 1 }: { color: string; scale?: number }) {
  const radius = TICK_MARKER_RADIUS * scale;
  const size = radius * 2;

  return (
    <Box
      style={{
        width: size,
        height: size,
        borderRadius: '50%',
        background: color,
        border: `${Math.max(1, scale)}px solid rgba(255, 255, 255, 0.25)`,
        boxSizing: 'border-box',
        flexShrink: 0,
      }}
    />
  );
}

function HitsoundAdditionPreview({ color, label }: { color: string; label: string }) {
  return (
    <Group gap={6} wrap="nowrap">
      <Box
        style={{
          width: 12,
          height: 12,
          borderRadius: '50%',
          background: color,
          border: '1px solid rgba(255, 255, 255, 0.35)',
          flexShrink: 0,
        }}
      />
      <Text size="xs" c="dimmed">
        {label}
      </Text>
    </Group>
  );
}

function PlayheadPreview() {
  return (
    <Box
      style={{
        width: 14,
        height: SOUND_STRIP_EDGE_LANE_HEIGHT - 4,
        position: 'relative',
        flexShrink: 0,
      }}
    >
      <Box
        style={{
          position: 'absolute',
          left: 6,
          top: 0,
          bottom: 0,
          width: 0,
          borderLeft: '2px solid var(--mantine-color-blue-4)',
          boxShadow: '0 0 8px rgba(56, 189, 248, 0.35)',
        }}
      />
    </Box>
  );
}

function SectionTintPreview({ color, label }: { color: string; label: string }) {
  return (
    <Group gap={6} wrap="nowrap">
      <Box
        style={{
          width: 28,
          height: 8,
          borderRadius: 2,
          background: withAlpha(color, SAMPLESET_OVERLAY_ALPHA),
          borderTop: `2px solid ${withAlpha(color, SAMPLESET_ACCENT_ALPHA)}`,
        }}
      />
      <Text size="xs" c="dimmed">
        {label}
      </Text>
    </Group>
  );
}

function BodyTintPreview({ color }: { color: string }) {
  return (
    <Box
      style={{
        width: 28,
        height: 6,
        borderRadius: 2,
        background: color,
      }}
    />
  );
}

function GapOverlayPreview({ color }: { color: string }) {
  return (
    <Box
      style={{
        width: 28,
        height: 10,
        borderRadius: 2,
        background: color,
      }}
    />
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
          flexWrap: 'wrap',
        }}
      >
        {children}
      </Box>
    </Group>
  );
}

export default function HitsoundLegendContent() {
  const sampleBanks = [
    { label: 'Normal', color: EDITOR_SAMPLE_BANK_COLORS.Normal },
    { label: 'Soft', color: EDITOR_SAMPLE_BANK_COLORS.Soft },
    { label: 'Drum', color: EDITOR_SAMPLE_BANK_COLORS.Drum },
  ] as const;

  const gapOverlayColor = withAlpha(HITSOUND_GAP_OVERLAY_COLOR, HITSOUND_GAP_OVERLAY_ALPHA);

  return (
    <Group gap={24} align="flex-start" wrap="wrap">
      <Stack gap={6}>
        <LanePreview label="Playhead" height={SOUND_STRIP_EDGE_LANE_HEIGHT}>
          <PlayheadPreview />
          <Text size="xs" c="dimmed">
            Seek cursor
          </Text>
        </LanePreview>

        <LanePreview label="Additions" height={SOUND_STRIP_EDGE_LANE_HEIGHT}>
          <HitsoundAdditionPreview color={HITSOUND_COLORS.none} label="None" />
          <HitsoundAdditionPreview color={HITSOUND_COLORS.whistle} label="Whistle" />
          <HitsoundAdditionPreview color={HITSOUND_COLORS.finish} label="Finish" />
          <HitsoundAdditionPreview color={HITSOUND_COLORS.clap} label="Clap" />
        </LanePreview>

        <LanePreview label="Strip bank" height={SOUND_STRIP_EDGE_LANE_HEIGHT}>
          {sampleBanks.map((item) => (
            <Group key={item.label} gap={6} wrap="nowrap">
              <EdgeMarkerPreview color={item.color} />
              <Text size="xs" c="dimmed">
                {item.label}
              </Text>
            </Group>
          ))}
        </LanePreview>

        <LanePreview label="Object body" height={SOUND_STRIP_EDGE_LANE_HEIGHT}>
          {sampleBanks.map((item) => (
            <Group key={item.label} gap={6} wrap="nowrap">
              <BodyTintPreview color={withAlpha(item.color, SAMPLESET_BODY_ALPHA)} />
              <Text size="xs" c="dimmed">
                {item.label}
              </Text>
            </Group>
          ))}
        </LanePreview>
      </Stack>

      <Stack gap={6}>
        <LanePreview
          label="Slider sounds"
          height={SOUND_STRIP_PASSIVE_LANE_HEIGHT + SOUND_STRIP_LANE_GAP}
        >
          <Group gap={8} wrap="nowrap" align="center">
            <BodyMarkerPreview color={EDITOR_SAMPLE_BANK_COLORS.Soft} />
            <Text size="xs" c="dimmed">
              Body slide
            </Text>
          </Group>
          <Group gap={8} wrap="nowrap" align="center">
            <BodyMarkerPreview
              color={EDITOR_SAMPLE_BANK_COLORS.Soft}
              additionColor={HITSOUND_COLORS.whistle}
            />
            <Text size="xs" c="dimmed">
              Body whistle
            </Text>
          </Group>
          <Group gap={8} wrap="nowrap" align="center">
            <TickMarkerPreview color={EDITOR_SAMPLE_BANK_COLORS.Drum} />
            <Text size="xs" c="dimmed">
              Tick
            </Text>
          </Group>
          <Text size="xs" c="dimmed">
            Fill is sample bank; outline is body addition
          </Text>
        </LanePreview>

        <Group gap={8} wrap="nowrap" align="flex-start">
          <Text size="xs" c="dimmed" w={92} style={{ flexShrink: 0, paddingTop: 2 }}>
            Section tint
          </Text>
          <Stack gap={4}>
            <Group gap="md" wrap="wrap">
              <SectionTintPreview
                color={EDITOR_SAMPLE_BANK_COLORS.Normal}
                label="Normal section"
              />
              <SectionTintPreview color={EDITOR_SAMPLE_BANK_COLORS.Soft} label="Soft section" />
              <SectionTintPreview color={EDITOR_SAMPLE_BANK_COLORS.Drum} label="Drum section" />
            </Group>
            <Text size="xs" c="dimmed">
              From timing points; Auto sections stay untinted
            </Text>
          </Stack>
        </Group>

        <LanePreview label="Gap overlay" height={SOUND_STRIP_EDGE_LANE_HEIGHT}>
          <GapOverlayPreview color={gapOverlayColor} />
          <Text size="xs" c="dimmed">
            Sparse hitsound feedback
          </Text>
        </LanePreview>
      </Stack>
    </Group>
  );
}
