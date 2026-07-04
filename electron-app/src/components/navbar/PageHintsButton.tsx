import { ActionIcon, Group, HoverCard, List, Text } from '@mantine/core';
import { IconInfoCircle } from '@tabler/icons-react';
import { useMemo } from 'react';
import { useLocation } from 'react-router-dom';
import { getPageHints } from './pageHints.tsx';
import { usePageHints } from '../../context/PageHintsContext.tsx';
import { useSettings } from '../../context/SettingsContext.tsx';
import { isMacPlatform } from '../../utils/platform.ts';

export default function PageHintsButton() {
  const location = useLocation();
  const { overviewTab, objectsHasHitsoundModes } = usePageHints();
  const { settings } = useSettings();
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

  if (hints.length === 0) {
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
        <ActionIcon color="gray.6" variant="subtle" size="xl" radius="xl" aria-label="Page tips">
          <IconInfoCircle color="var(--mantine-color-white)" />
        </ActionIcon>
      </HoverCard.Target>
      <HoverCard.Dropdown p="sm" style={{ width: 'max-content', maxWidth: 360 }}>
        <Group gap={6} mb={6} wrap="nowrap" align="center">
          <IconInfoCircle size={14} color="var(--mantine-color-dimmed)" aria-hidden />
          <Text size="xs" fw={600} tt="uppercase" c="dimmed">
            Tips
          </Text>
        </Group>
        <List size="sm" spacing={6} withPadding style={{ direction: 'ltr' }}>
          {hints.map((hint) => (
            <List.Item key={hint.id}>{hint.content}</List.Item>
          ))}
        </List>
      </HoverCard.Dropdown>
    </HoverCard>
  );
}
