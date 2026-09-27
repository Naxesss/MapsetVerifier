import { ActionIcon, Group, HoverCard, List } from '@mantine/core';
import { IconInfoCircle } from '@tabler/icons-react';
import { useMemo } from 'react';
import { useLocation } from 'react-router-dom';
import { getActiveNavRoute, NAV_CONTROL_SIZE } from './navConfig.ts';
import { getPageHints } from './pageHints.tsx';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { usePageHints } from '../../context/PageHintsContext.tsx';
import { useSettings } from '../../context/SettingsContext.tsx';
import { isMacPlatform } from '../../utils/platform.ts';
import { MicroLabel } from '../common/Headings.tsx';

const MAPSET_ROUTES = ['/checks', '/snapshots', '/overview'];

export default function PageHintsButton() {
  const location = useLocation();
  const { overviewTab, objectsHasHitsoundModes } = usePageHints();
  const { settings } = useSettings();
  const { selectedFolder } = useBeatmap();
  const isMac = useMemo(() => isMacPlatform(), []);
  const hints = getPageHints(
    location.pathname,
    overviewTab,
    objectsHasHitsoundModes,
    isMac,
    settings.showMinor,
    settings.bookmarksEnabled,
    settings.showCheckRunDelta
  );

  // Mapset pages show an empty state until a mapset is picked; their tips don't apply yet.
  const needsMapset = MAPSET_ROUTES.includes(getActiveNavRoute(location.pathname));

  if (hints.length === 0 || (needsMapset && !selectedFolder)) {
    return null;
  }

  return (
    <HoverCard
      shadow="md"
      position="bottom-end"
      openDelay={120}
      closeDelay={80}
      withinPortal
      offset={{ mainAxis: 12, crossAxis: 40 }}
    >
      <HoverCard.Target>
        <ActionIcon color="gray" variant="subtle" size={NAV_CONTROL_SIZE} aria-label="Page tips">
          <IconInfoCircle color="var(--mantine-color-white)" />
        </ActionIcon>
      </HoverCard.Target>
      <HoverCard.Dropdown p="sm" style={{ width: 'max-content', maxWidth: 360 }}>
        <Group gap="xs" mb="xs" wrap="nowrap" align="center">
          <IconInfoCircle size={14} color="var(--mantine-color-dimmed)" aria-hidden />
          <MicroLabel>Tips</MicroLabel>
        </Group>
        <List size="sm" spacing="xs" withPadding style={{ direction: 'ltr' }}>
          {hints.map((hint) => (
            <List.Item key={hint.id}>{hint.content}</List.Item>
          ))}
        </List>
      </HoverCard.Dropdown>
    </HoverCard>
  );
}
