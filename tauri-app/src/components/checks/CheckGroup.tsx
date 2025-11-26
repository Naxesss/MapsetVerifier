import { Stack, Group, Text } from '@mantine/core';
import { IconChevronDown, IconChevronRight } from '@tabler/icons-react';
import React from 'react';
import IssueRow from './IssueRow';
import { ApiCheckResult, Level } from '../../Types';
import LevelIcon from '../icons/LevelIcon.tsx';

interface CheckGroupProps {
  id: number;
  items: ApiCheckResult[];
  name?: string;
}

const CheckGroup: React.FC<CheckGroupProps> = ({ id, items, name }) => {
  // Determine the highest severity per requested order: Error > Problem > Warning > Minor > Info
  const severityOrder: Level[] = ['Error', 'Problem', 'Warning', 'Minor', 'Info'];
  // Treat 'Check' as 'Info' (fallback) if present
  const normalizedLevels = items.map((i) => (i.level === 'Check' ? 'Info' : i.level)) as Level[];
  let highest: Level = 'Info';
  for (const level of severityOrder) {
    if (normalizedLevels.includes(level)) {
      highest = level;
      break;
    }
  }

  const [open, setOpen] = React.useState(true);
  const [showAll, setShowAll] = React.useState(false);
  const VISIBLE_COUNT = 5;
  const toggle = () => setOpen((o) => !o);
  const toggleShowAll = () => setShowAll((s) => !s);
  const onKeyDown: React.KeyboardEventHandler = (e) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      toggle();
    }
  };

  const visibleItems = showAll ? items : items.slice(0, VISIBLE_COUNT);
  const extraCount = items.length - VISIBLE_COUNT;

  return (
    <Stack gap="0" p="xs" justify="center">
      <Group justify="space-between" wrap="nowrap">
        <Group
          gap="xs"
          wrap="nowrap"
          align="center"
          onClick={toggle}
          onKeyDown={onKeyDown}
          role="button"
          tabIndex={0}
          aria-expanded={open}
          style={{ cursor: 'pointer', userSelect: 'none' }}
        >
          {open ? <IconChevronDown size={16} /> : <IconChevronRight size={16} />}
          <LevelIcon level={highest} size={16} />
          <Text size="sm" fw="bold">
            {name}
          </Text>
        </Group>
      </Group>

      {open && (
        <>
          {visibleItems.map((item, idx) => (
            <IssueRow key={`${id}-${idx}`} item={item} />
          ))}
          {extraCount > 0 && (
            <Text
              ml="xl"
              size="sm"
              role="button"
              tabIndex={0}
              onClick={toggleShowAll}
              style={{ cursor: 'pointer', color: 'var(--mantine-color-blue-6)', fontWeight: 500 }}
            >
              {showAll ? `Hide extra issues` : `Show ${extraCount} more`}
            </Text>
          )}
        </>
      )}
    </Stack>
  );
};

export default CheckGroup;
