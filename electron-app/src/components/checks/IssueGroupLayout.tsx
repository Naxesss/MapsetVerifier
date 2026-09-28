import { Collapse, Flex, Stack, Text } from '@mantine/core';
import { IconChevronRight } from '@tabler/icons-react';
import ShowMoreButton from './ShowMoreButton';
import LevelIcon from '../icons/LevelIcon';
import type { DisplayLevel } from './utils/levelUtils';
import type { KeyboardEvent, ReactNode } from 'react';

/** Issues shown before "Show N more". */
export const VISIBLE_ISSUE_COUNT = 5;

// Header: chevron (16px), gap (4px), level icon (16px), gap (4px), then the name at 40px.
/** Centre of the chevron, where the guide line runs. */
const GUIDE_LINE_OFFSET = 8;
/** Where issue rows start; plus their 4px padding, their icons sit at 40px, under the check name. */
const ISSUE_INDENT = 36;

interface IssueGroupLayoutProps<T> {
  /** The check's name. */
  name: ReactNode;
  /** The group's most severe level. */
  level: DisplayLevel;
  /** Shown between the level and the name, e.g. the difficulty in a mapset-wide list. */
  badge?: ReactNode;
  /** Controls on the right of the header, e.g. copying the group. */
  actions?: ReactNode;
  items: T[];
  renderItem: (item: T, index: number) => ReactNode;
  isOpen: boolean;
  onToggleOpen: () => void;
  showAll: boolean;
  onToggleShowAll: () => void;
  id?: string;
}

/**
 * One check's issues: a header that folds the group, the first issues under the check's name with
 * a guide line down from the chevron, and "Show N more" for the rest. Used by the check results
 * and by the "changes since the last run" list, so both read the same.
 */
export default function IssueGroupLayout<T>({
  name,
  level,
  badge,
  actions,
  items,
  renderItem,
  isOpen,
  onToggleOpen,
  showAll,
  onToggleShowAll,
  id,
}: IssueGroupLayoutProps<T>) {
  const firstItems = items.slice(0, VISIBLE_ISSUE_COUNT);
  const extraItems = items.slice(VISIBLE_ISSUE_COUNT);

  const onKeyDown = (event: KeyboardEvent) => {
    // Keys pressed on a button in the header (e.g. Copy all) are that button's own.
    if (event.target !== event.currentTarget) return;
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      onToggleOpen();
    }
  };

  return (
    <Stack gap={0} justify="center" id={id}>
      <Flex
        gap="xs"
        align="center"
        onClick={onToggleOpen}
        onKeyDown={onKeyDown}
        role="button"
        tabIndex={0}
        aria-expanded={isOpen}
        style={{ cursor: 'pointer', userSelect: 'none', minWidth: 0 }}
      >
        <span
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            flexShrink: 0,
            transform: isOpen ? 'rotate(90deg)' : 'rotate(0deg)',
            transition: 'transform 200ms ease',
          }}
        >
          <IconChevronRight size={16} />
        </span>
        <LevelIcon level={level} size={16} />
        {badge}
        <Text size="sm" fw={700} style={{ minWidth: 0, overflowWrap: 'anywhere' }}>
          {name}
        </Text>
        {actions && (
          <Flex ml="auto" style={{ flexShrink: 0 }} onClick={(event) => event.stopPropagation()}>
            {actions}
          </Flex>
        )}
      </Flex>

      <Collapse in={isOpen}>
        <Stack
          gap={0}
          style={{
            marginLeft: GUIDE_LINE_OFFSET,
            paddingLeft: ISSUE_INDENT - GUIDE_LINE_OFFSET - 1,
            borderLeft: '1px solid var(--mantine-color-default-border)',
          }}
        >
          {firstItems.map((item, index) => renderItem(item, index))}
          {/* Rendered only once asked for: large groups would otherwise draw every row up front. */}
          {showAll &&
            extraItems.map((item, index) => renderItem(item, VISIBLE_ISSUE_COUNT + index))}
          {extraItems.length > 0 && (
            <ShowMoreButton
              expanded={showAll}
              hiddenCount={extraItems.length}
              onToggle={onToggleShowAll}
            />
          )}
        </Stack>
      </Collapse>
    </Stack>
  );
}
