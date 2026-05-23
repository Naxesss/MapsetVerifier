import { Box, Group, List, Modal, Stack, Text, Title } from '@mantine/core';
import { HITSOUND_COLORS } from '../hitsoundUtils.ts';

type ObjectsTimelineHelpModalProps = {
  opened: boolean;
  onClose: () => void;
  showHitsoundSection?: boolean;
};

function ColorSwatch({ color, label }: { color: string; label: string }) {
  return (
    <Group gap={6} wrap="nowrap">
      <Box
        style={{
          width: 10,
          height: 10,
          borderRadius: 999,
          background: color,
          flexShrink: 0,
        }}
      />
      <Text size="sm">{label}</Text>
    </Group>
  );
}

export default function ObjectsTimelineHelpModal({
  opened,
  onClose,
  showHitsoundSection = true,
}: ObjectsTimelineHelpModalProps) {
  return (
    <Modal opened={opened} onClose={onClose} title="How to use the timeline" size="lg" centered>
      <Stack gap="lg">
        <Stack gap="xs">
          <Title order={5}>Navigation</Title>
          <List size="sm" spacing={4}>
            <List.Item>Drag horizontally in the timeline area to pan.</List.Item>
            <List.Item>Use the zoom slider to stretch or compress time.</List.Item>
            <List.Item>Drag the grip on a row to reorder difficulties.</List.Item>
            <List.Item>Use the eye icons to show or hide individual rows.</List.Item>
            <List.Item>
              Open <strong>Full view</strong> for a larger timeline. Pan position is kept when you
              switch between inline and full view.
            </List.Item>
            <List.Item>
              Right-click an object to copy its editor timestamp or open it in the osu! editor.
            </List.Item>
          </List>
        </Stack>

        <Stack gap="xs">
          <Title order={5}>Structure view</Title>
          <Text size="sm" c="dimmed">
            The default view. Shows object shapes, combo colours, breaks, and the timing grid. Use
            the style dropdown to change how objects are drawn.
          </Text>
        </Stack>

        {showHitsoundSection && (
          <>
            <Stack gap="xs">
              <Title order={5}>Hitsounding view</Title>
              <Text size="sm" c="dimmed">
                Available for osu! and osu!catch. Open full view, then switch with the Structure /
                Hitsounding control.
              </Text>
              <List size="sm" spacing={4}>
                <List.Item>
                  Object circle colours show only <strong>hitsound additions</strong> (Normal,
                  Whistle, Clap, Finish). Priority is Finish → Clap → Whistle → Normal: the highest
                  priority addition fills the circle; when stacked, the next one appears as an outer
                  ring.
                </List.Item>
                <List.Item>
                  Slider bodies stay neutral grey in this view — edge hitsounds are shown on
                  circles at heads, tails, and reverses.
                </List.Item>
              </List>
            </Stack>

            <Stack gap="xs">
              <Title order={5}>Seeking and floating panel</Title>
              <List size="sm" spacing={4}>
                <List.Item>
                  A fixed blue playhead marks the <strong>first note</strong> across all visible
                  difficulties. It does not follow the mouse.
                </List.Item>
                <List.Item>
                  Drag the timeline horizontally to seek under the playhead. The floating panel
                  updates to show the nearest object edge at that time for each difficulty.
                </List.Item>
                <List.Item>
                  Drag the panel header to reposition it. The body scrolls when many difficulties
                  are visible. Difficulty names are coloured by star rating.
                </List.Item>
                <List.Item>
                  Under <strong>Object circle</strong>, the panel lists the same stacked additions
                  as the drawn circle. Hover a name for a short dominant / secondary tooltip; a
                  secondary addition is shown with dimmed text when stacked.
                </List.Item>
                <List.Item>
                  <strong>Sample file</strong> shows the sample bank and custom index (e.g. Soft,
                  Drum, #4). That line describes which audio file plays — it does{' '}
                  <strong>not</strong> affect circle colour.
                </List.Item>
              </List>
            </Stack>

            <Stack gap="xs">
              <Title order={5}>Sound strip</Title>
              <Text size="sm" c="dimmed">
                The lane below each row is a compact timeline of when samples play.
              </Text>
              <List size="sm" spacing={4}>
                <List.Item>
                  <strong>Edge lane</strong> — coloured vertical bars at edge hitsounds, using the
                  same colours as object circles. Helps scan hitsound patterns across time.
                </List.Item>
                <List.Item>
                  <strong>Passive lane</strong> — neutral dash for slider body samples, dot for
                  slider ticks. These are the main visual for body/tick feedback because slider
                  bodies are not coloured in hitsounding view.
                </List.Item>
                <List.Item>
                  Edge bar colour reflects hitsound type only (fixed bar size). Sample bank does
                  not change individual strip markers — it tints the whole row instead (see below).
                </List.Item>
              </List>
            </Stack>

            <Stack gap="xs">
              <Title order={5}>Layer toggles</Title>
              <Text size="sm" c="dimmed">
                In full view hitsounding mode.
              </Text>
              <List size="sm" spacing={4}>
                <List.Item>
                  <strong>Body sounds</strong> — repeating slider body samples in the passive lane.
                </List.Item>
                <List.Item>
                  <strong>Ticks</strong> — slider tick samples in the passive lane.
                </List.Item>
                <List.Item>
                  <strong>Sample bank</strong> — tints the full row for timing sections that use
                  Soft or Drum sample banks (set on timing points). Normal sections stay untinted.
                </List.Item>
                <List.Item>
                  <strong>Gap overlay</strong> — orange regions where hitsound feedback is sparse,
                  similar to the osu! editor warning.
                </List.Item>
              </List>
            </Stack>

            <Stack gap="xs">
              <Title order={5}>Sound strip key</Title>
              <Group gap="md" wrap="wrap">
                <ColorSwatch color={HITSOUND_COLORS.normal} label="Edge · Normal" />
                <ColorSwatch color={HITSOUND_COLORS.whistle} label="Edge · Whistle" />
                <ColorSwatch color={HITSOUND_COLORS.clap} label="Edge · Clap" />
                <ColorSwatch color={HITSOUND_COLORS.finish} label="Edge · Finish" />
                <ColorSwatch color={HITSOUND_COLORS.body} label="Passive · Body dash" />
                <ColorSwatch color={HITSOUND_COLORS.tick} label="Passive · Tick dot" />
              </Group>
            </Stack>
          </>
        )}
      </Stack>
    </Modal>
  );
}
