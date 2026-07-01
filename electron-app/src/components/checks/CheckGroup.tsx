import { ActionIcon, Collapse, Flex, Stack, Text, Tooltip } from '@mantine/core';
import { IconChevronRight, IconInfoCircleFilled } from '@tabler/icons-react';
import React, { useMemo, useState } from 'react';
import IssueRow from './IssueRow';
import { ApiCheckResult, Level } from '../../Types';
import DocumentationCheckModal from '../documentation/DocumentationCheckModal';
import { useDocumentationChecks } from '../documentation/hooks/useDocumentationChecks';
import LevelIcon from '../icons/LevelIcon.tsx';

interface CheckGroupProps {
  id: number;
  items: ApiCheckResult[];
  isOpen: boolean;
  name?: string;
  showAll: boolean;
  onToggleOpen: (id: number) => void;
  onToggleShowAll: (id: number) => void;
}

const VISIBLE_COUNT = 5;

const CheckGroup: React.FC<CheckGroupProps> = ({
  id,
  items,
  isOpen,
  name,
  showAll,
  onToggleOpen,
  onToggleShowAll,
}) => {
  // Determine the highest severity per requested order: Error > Problem > Warning > Minor > Info
  const highest = useMemo((): Level => {
    const severityOrder: Level[] = ['Error', 'Problem', 'Warning', 'Minor', 'Info'];
    const normalizedLevels = items.map((i) => (i.level === 'Check' ? 'Info' : i.level)) as Level[];

    for (const level of severityOrder) {
      if (normalizedLevels.includes(level)) {
        return level;
      }
    }

    return 'Info';
  }, [items]);

  const [docModalOpen, setDocModalOpen] = useState(false);
  const { getCheckById } = useDocumentationChecks();
  const documentationCheck = getCheckById(id);

  const toggle = () => onToggleOpen(id);
  const toggleShowAll = () => onToggleShowAll(id);
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
      <Flex justify="space-between" wrap="nowrap" align="center">
        <Flex
          gap="xs"
          onClick={toggle}
          onKeyDown={onKeyDown}
          role="button"
          tabIndex={0}
          aria-expanded={isOpen}
          style={{ cursor: 'pointer', userSelect: 'none' }}
        >
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              transform: isOpen ? 'rotate(90deg)' : 'rotate(0deg)',
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

      <Collapse in={isOpen}>
        <Stack ml="xl" gap="0">
          {firstItems.map((item, idx) => (
            <IssueRow key={`${id}-${idx}`} item={item} />
          ))}
          {showAll && (
            <Collapse in={showAll}>
              <Stack gap="0">
                {extraItems.map((item, idx) => (
                  <IssueRow key={`${id}-${VISIBLE_COUNT + idx}`} item={item} />
                ))}
              </Stack>
            </Collapse>
          )}
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

export default React.memo(CheckGroup);
