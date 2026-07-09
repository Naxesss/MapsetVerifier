import { Kbd, List, Modal, Stack, Text, Title } from '@mantine/core';
import HitsoundLegendContent from './HitsoundLegendContent.tsx';

type ObjectsTimelineHelpModalProps = {
  opened: boolean;
  onClose: () => void;
  showHitsoundSection?: boolean;
};

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
            <List.Item>Drag horizontally to pan.</List.Item>
            <List.Item>
              Hold <Kbd size="xs">Shift</Kbd> + scroll to step through timing snap ticks.
            </List.Item>
            <List.Item>
              Hold <Kbd size="xs">Ctrl</Kbd> + scroll to zoom in or out. Holding both keys together
              does nothing.
            </List.Item>
            <List.Item>Drag the grip on a row to reorder difficulties.</List.Item>
            <List.Item>Use the eye icons to show or hide individual rows.</List.Item>
            <List.Item>
              Right-click an object to copy its timestamp or open it in the editor.
            </List.Item>
          </List>
        </Stack>

        <Stack gap="xs">
          <Title order={5}>Structure view</Title>
          <Text size="sm" c="dimmed">
            Default view, object shapes, combo colours, breaks, and the timing grid. Change the
            object style from the dropdown.
          </Text>
        </Stack>

        {showHitsoundSection && (
          <>
            <Stack gap="xs">
              <Title order={5}>Hitsounding view</Title>
              <Text size="sm" c="dimmed">
                osu! and osu!catch only. Switch via Structure / Hitsounding in the header.
              </Text>
              <List size="sm" spacing={4}>
                <List.Item>
                  Circles show <strong>additions</strong> (Whistle, Clap, Finish; grey = none).
                  Priority is Finish → Clap → Whistle, the top one fills the marker, stacked ones
                  show as outer rings.
                </List.Item>
                <List.Item>
                  Slider/spinner body tint is the <strong>sample bank</strong> (Normal, Soft, Drum)
                  for sliderslide.
                </List.Item>
                <List.Item>
                  Hover an object or edge for details; right-click to copy/edit.
                </List.Item>
              </List>
            </Stack>

            <Stack gap="xs">
              <Title order={5}>Hitsound details</Title>
              <List size="sm" spacing={4}>
                <List.Item>
                  <strong>Hitsound additions</strong>, matches the circles above the strip. Hover a
                  name for primary/stacked tooltips.
                </List.Item>
                <List.Item>
                  <strong>Sample bank</strong>, the hitnormal bank at that edge, plus custom index
                  when set. Strip bars use the same value per edge.
                </List.Item>
              </List>
            </Stack>

            <Stack gap="xs">
              <Title order={5}>Sound strip</Title>
              <Text size="sm" c="dimmed">
                The lane below each row, a compact timeline of when samples play.
              </Text>
              <List size="sm" spacing={4}>
                <List.Item>
                  <strong>Edge lane</strong>, bars at each object edge (head, tail, reverse),
                  coloured by hitnormal sample bank.
                </List.Item>
                <List.Item>
                  <strong>Passive lane</strong>, dash for sliderslide, dot for slidertick, plus a
                  second dash for sliderwhistle when body whistle is on.
                </List.Item>
                <List.Item>
                  Circles show additions, bars show the underlying bank, they can differ on the same
                  hit (e.g. Clap circle with Soft bar).
                </List.Item>
              </List>
            </Stack>

            <Stack gap="xs">
              <Title order={5}>Layer toggles</Title>
              <List size="sm" spacing={4}>
                <List.Item>
                  <strong>Body sounds</strong>, repeating slider body samples in the passive lane.
                </List.Item>
                <List.Item>
                  <strong>Ticks</strong>, slider tick samples in the passive lane.
                </List.Item>
                <List.Item>
                  <strong>Sample bank</strong>, row tint for timing sections set to Normal, Soft, or
                  Drum. Auto sections stay untinted.
                </List.Item>
                <List.Item>
                  <strong>Gap overlay</strong>, magenta regions with sparse hitsound feedback.
                </List.Item>
              </List>
            </Stack>

            <Stack gap="xs">
              <Title order={5}>Colour key</Title>
              <HitsoundLegendContent />
            </Stack>
          </>
        )}
      </Stack>
    </Modal>
  );
}
