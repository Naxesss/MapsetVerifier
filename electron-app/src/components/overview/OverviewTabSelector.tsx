import { Box, Group, UnstyledButton } from '@mantine/core';
import {
  IconAdjustmentsHorizontal,
  IconChartLine,
  IconMusic,
  IconTags,
  IconTimeline,
  IconVideo,
  type Icon,
} from '@tabler/icons-react';
import { useRef, type KeyboardEvent } from 'react';
import type { OverviewTab } from '../navbar/pageHints.tsx';

interface OverviewTabSelectorProps {
  tabs: OverviewTab[];
  value: OverviewTab;
  onChange: (tab: OverviewTab) => void;
}

const TAB_ICONS: Record<OverviewTab, Icon> = {
  Metadata: IconTags,
  Objects: IconTimeline,
  Beatmap: IconAdjustmentsHorizontal,
  Difficulty: IconChartLine,
  Audio: IconMusic,
  Video: IconVideo,
};

/** Room between the tallest segment and its click area's edge, as in the difficulty picker. */
const SEGMENT_HIT_SLACK = 5;

/**
 * The Overview's sections as equal-width tabs across the header, each standing on its segment of
 * the same track the difficulty picker draws under its buttons (`.mv-overview-tab` in
 * global.scss). The 36px row plus the track makes the header exactly as tall as on Checks and
 * Snapshots, so the banner doesn't move when switching pages. Follows the WAI-ARIA tabs pattern:
 * one tab stop, arrow keys move and select, Home and End jump.
 */
export default function OverviewTabSelector({ tabs, value, onChange }: OverviewTabSelectorProps) {
  const tabRefs = useRef<(HTMLButtonElement | null)[]>([]);

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const index = tabs.indexOf(value);
    let next: number;
    switch (event.key) {
      case 'ArrowLeft':
        next = (index - 1 + tabs.length) % tabs.length;
        break;
      case 'ArrowRight':
        next = (index + 1) % tabs.length;
        break;
      case 'Home':
        next = 0;
        break;
      case 'End':
        next = tabs.length - 1;
        break;
      default:
        return;
    }
    event.preventDefault();
    onChange(tabs[next]);
    tabRefs.current[next]?.focus();
  };

  return (
    <Group
      gap={3}
      wrap="nowrap"
      role="tablist"
      aria-label="Overview sections"
      mb={-SEGMENT_HIT_SLACK}
      onKeyDown={handleKeyDown}
    >
      {tabs.map((tab, i) => {
        const selected = tab === value;
        const TabIcon = TAB_ICONS[tab];

        return (
          <UnstyledButton
            key={tab}
            ref={(el) => {
              tabRefs.current[i] = el;
            }}
            className="mv-overview-tab"
            role="tab"
            id={`overview-tab-${tab}`}
            aria-selected={selected}
            aria-controls="overview-panel"
            tabIndex={selected ? 0 : -1}
            data-active={selected || undefined}
            onClick={() => onChange(tab)}
          >
            <span className="mv-overview-tab__label">
              <TabIcon className="mv-overview-tab__icon" size={16} stroke={1.8} aria-hidden />
              <span className="mv-overview-tab__text">{tab}</span>
            </span>
            <Box component="span" className="mv-overview-tab__track" aria-hidden>
              <span className="mv-overview-tab__bar" />
            </Box>
          </UnstyledButton>
        );
      })}
    </Group>
  );
}
