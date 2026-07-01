import { Collapse, Flex, Stack, Text } from '@mantine/core';
import { IconChevronRight } from '@tabler/icons-react';
import React, { useState } from 'react';
import IssueDetailDrawer from './IssueDetailDrawer';
import IssueRow from './IssueRow';
import { ApiCheckResult, Level } from '../../Types';
import { useDocumentationChecks } from '../documentation/hooks/useDocumentationChecks';
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

  const [open, setOpen] = useState(true);
  const [showAll, setShowAll] = useState(false);
  const [selectedIssue, setSelectedIssue] = useState<ApiCheckResult | null>(null);
  const { getCheckById } = useDocumentationChecks();
  const documentationCheck = getCheckById(id);

  const VISIBLE_COUNT = 5;
  const toggle = () => setOpen((o) => !o);
  const toggleShowAll = () => setShowAll((s) => !s);
  const onKeyDown: React.KeyboardEventHandler = (e) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      toggle();
    }
  };

  const firstItems = items.slice(0, VISIBLE_COUNT);
  const extraItems = items.slice(VISIBLE_COUNT);
  const extraCount = extraItems.length;

  return (
    <Stack gap="0" justify="center">
      <Flex
        gap="xs"
        onClick={toggle}
        onKeyDown={onKeyDown}
        role="button"
        tabIndex={0}
        aria-expanded={open}
        style={{ cursor: 'pointer', userSelect: 'none' }}
      >
        <span
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            transform: open ? 'rotate(90deg)' : 'rotate(0deg)',
            transition: 'transform 200ms ease',
          }}
        >
          <IconChevronRight size={16} />
        </span>
        <LevelIcon level={highest} size={16} />
        <Text size="sm" fw="bold">
          {name}
        </Text>
      </Flex>

      <Collapse in={open}>
        <Stack ml="xl" gap="0">
          {firstItems.map((item, idx) => (
            <IssueRow key={`${id}-${idx}`} item={item} onOpen={() => setSelectedIssue(item)} />
          ))}
          <Collapse in={showAll}>
            <Stack gap="0">
              {extraItems.map((item, idx) => (
                <IssueRow
                  key={`${id}-${VISIBLE_COUNT + idx}`}
                  item={item}
                  onOpen={() => setSelectedIssue(item)}
                />
              ))}
            </Stack>
          </Collapse>
          {extraCount > 0 && (
            <Text
              size="sm"
              role="button"
              tabIndex={0}
              onClick={toggleShowAll}
              style={{ cursor: 'pointer', color: 'var(--mantine-color-blue-6)', fontWeight: 500 }}
            >
              {showAll ? `Hide extra issues` : `Show ${extraCount} more`}
            </Text>
          )}
        </Stack>
      </Collapse>

      <IssueDetailDrawer
        opened={selectedIssue !== null}
        onClose={() => setSelectedIssue(null)}
        issue={selectedIssue}
        checkName={name}
        documentationCheck={documentationCheck}
      />
    </Stack>
  );
};

export default CheckGroup;
