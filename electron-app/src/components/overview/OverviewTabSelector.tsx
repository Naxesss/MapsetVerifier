import { alpha, Button, Group, Text, useMantineTheme } from '@mantine/core';
import {
  IconAdjustmentsHorizontal,
  IconChartLine,
  IconMusic,
  IconTags,
  IconTimeline,
  IconVideo,
  type Icon,
} from '@tabler/icons-react';
import type { OverviewTab } from '../navbar/pageHints.tsx';

const TAB_ICONS: Record<OverviewTab, Icon> = {
  Metadata: IconTags,
  Objects: IconTimeline,
  Beatmap: IconAdjustmentsHorizontal,
  Difficulty: IconChartLine,
  Audio: IconMusic,
  Video: IconVideo,
};

interface OverviewTabSelectorProps {
  tabs: OverviewTab[];
  value: OverviewTab;
  onChange: (tab: OverviewTab) => void;
}

/**
 * The Overview's sections, shown in the header the way Checks and Snapshots show their difficulty
 * selector: the same translucent strip and the same buttons as its "General" button.
 */
export default function OverviewTabSelector({ tabs, value, onChange }: OverviewTabSelectorProps) {
  const theme = useMantineTheme();
  const buttonBg = alpha(theme.colors.dark[4], 0.6);
  const buttonHover = alpha(theme.colors.dark[3], 0.7);

  return (
    <Group
      p="xs"
      gap="xs"
      w="fit-content"
      bg="hsl(200deg 10% 10% / 50%)"
      role="tablist"
      aria-label="Overview sections"
      style={{ borderRadius: theme.radius.md }}
    >
      {tabs.map((tab) => {
        const TabIcon = TAB_ICONS[tab];
        const active = tab === value;

        return (
          <Button
            key={tab}
            role="tab"
            aria-selected={active}
            variant="light"
            size="compact-md"
            h="fit-content"
            p="xs"
            style={{ '--button-bg': buttonBg, '--button-hover': buttonHover }}
            bd={active ? `1px solid ${theme.colors.dark[2]}` : '1px solid transparent'}
            leftSection={<TabIcon size={18} color="var(--mantine-color-white)" />}
            onClick={() => onChange(tab)}
          >
            <Text c="white">{tab}</Text>
          </Button>
        );
      })}
    </Group>
  );
}
