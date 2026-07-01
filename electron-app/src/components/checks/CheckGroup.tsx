import { ActionIcon, Collapse, Flex, Stack, Text, Tooltip } from '@mantine/core';
import { IconChevronRight, IconInfoCircleFilled } from '@tabler/icons-react';
import React, { useState } from 'react';
import IssueRow from './IssueRow';
import { ApiCheckResult, Level } from '../../Types';
import DocumentationCheckModal from '../documentation/DocumentationCheckModal';
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
  const [docModalOpen, setDocModalOpen] = useState(false);
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
    <Stack gap="0" justify="center" id={`check-group-${id}`}>
      <Flex justify="space-between" wrap="nowrap" align="center">
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
        {documentationCheck && (
          <Tooltip label="View documentation">
            <ActionIcon
              variant="subtle"
              onClick={(e) => {
                e.stopPropagation();
                setDocModalOpen(true);
              }}
            >
              <IconInfoCircleFilled size={16} />
            </ActionIcon>
          </Tooltip>
        )}
      </Flex>

      <Collapse in={open}>
        <Stack ml="xl" gap="0">
          {firstItems.map((item, idx) => (
            <IssueRow key={`${id}-${idx}`} item={item} />
          ))}
          <Collapse in={showAll}>
            <Stack gap="0">
              {extraItems.map((item, idx) => (
                <IssueRow key={`${id}-${VISIBLE_COUNT + idx}`} item={item} />
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

      {documentationCheck && (
        <DocumentationCheckModal
          opened={docModalOpen}
          onClose={() => setDocModalOpen(false)}
          check={documentationCheck}
        />
      )}
    </Stack>
  );
};

export default CheckGroup;
