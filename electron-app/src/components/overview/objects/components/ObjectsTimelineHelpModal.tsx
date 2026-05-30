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
            <List.Item>Drag horizontally in the timeline area to pan.</List.Item>
            <List.Item>
              Hold <Kbd size="xs">Shift</Kbd> over the timeline to enter{' '}
              <strong>timeline scroll mode</strong>. While <Kbd size="xs">Shift</Kbd> is held,
              scroll the wheel to step one timing snap tick forward (scroll down) or backward
              (scroll up). Horizontal trackpad scrolling over the timeline also steps snap ticks.
            </List.Item>
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
                  <strong>Circles on objects</strong> show <strong>hitsound additions</strong>{' '}
                  (Whistle, Clap, Finish). No addition is grey. Priority is Finish → Clap → Whistle:
                  the highest-priority addition fills the marker; stacked additions appear as outer
                  rings. Finish and Clap reuse the Soft and Drum hues; Whistle uses green.
                </List.Item>
                <List.Item>
                  <strong>Slider and spinner bodies</strong> — the path tint is the object{' '}
                  <strong>sample bank</strong> for sliderslide (Normal, Soft, or Drum). Every slider
                  has a sliderslide; only body-whistle sliders also get a second dash in the passive
                  lane.
                </List.Item>
                <List.Item>
                  Hover an object or edge for hitsound details. Right-click to copy a timestamp or
                  open the osu! editor.
                </List.Item>
              </List>
            </Stack>

            <Stack gap="xs">
              <Title order={5}>Hitsound details</Title>
              <List size="sm" spacing={4}>
                <List.Item>
                  <strong>Hitsound additions</strong> matches the circles above the strip. Hover a
                  name for primary / stacked tooltips.
                </List.Item>
                <List.Item>
                  <strong>Sample bank</strong> is the hitnormal bank at that edge (plus custom index
                  when set). Strip bars use the same value, resolved per object edge.
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
                  <strong>Edge lane</strong> — short vertical bars at each object edge (head, tail,
                  reverse). Colour is the hitnormal <strong>sample bank</strong>, not the addition
                  overlay sample.
                </List.Item>
                <List.Item>
                  <strong>Passive lane</strong> — dash for sliderslide (object bank) and, when
                  present, a second dash for sliderwhistle (addition bank). Dot for slidertick. Both
                  dashes can appear at the same timing line when body whistle is enabled.
                </List.Item>
                <List.Item>
                  Circles above the strip show additions; bars in the strip show the underlying
                  hitnormal bank. They can differ on the same hit (e.g. Clap circle with Soft bar).
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
                  <strong>Sample bank</strong> — row wash for timing sections set to Normal, Soft,
                  or Drum on timing points. Auto sections stay untinted.
                </List.Item>
                <List.Item>
                  <strong>Gap overlay</strong> — magenta regions where hitsound feedback is sparse,
                  similar to the osu! editor warning.
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
