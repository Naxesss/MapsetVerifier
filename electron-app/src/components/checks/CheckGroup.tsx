import { Button, Group, Modal, Stack, Text } from '@mantine/core';
import { IconCopy } from '@tabler/icons-react';
import React, { useMemo, useState } from 'react';
import IssueDetailDrawer, { copyToClipboard } from './IssueDetailDrawer';
import IssueGroupLayout from './IssueGroupLayout';
import IssueRow from './IssueRow';
import { getHighestIssueLevel, normalizeLevel } from './utils/levelUtils';
import { Z_INDEX } from '../../theme/layers';
import { ApiCheckResult } from '../../Types';
import { countWord } from '../../utils/countWord';
import { getLevelLabel } from '../../utils/levelLabel';
import { useDocumentationChecks } from '../documentation/hooks/useDocumentationChecks';

interface CheckGroupProps {
  id: number;
  items: ApiCheckResult[];
  isOpen: boolean;
  name?: string;
  showAll: boolean;
  onToggleOpen: (id: number) => void;
  onToggleShowAll: (id: number) => void;
}

const LARGE_GROUP_THRESHOLD = 25;

export function getGroupCopyText(items: ApiCheckResult[], groupName?: string) {
  const title = groupName ? groupName : 'Issues';
  const lines = items.map((item) => `- ${item.message}`);
  return [title, ...lines].join('\n');
}

const CheckGroup: React.FC<CheckGroupProps> = ({
  id,
  items,
  isOpen,
  name,
  showAll,
  onToggleOpen,
  onToggleShowAll,
}) => {
  const highest = useMemo(() => getHighestIssueLevel(items.map((item) => item.level)), [items]);

  const [selectedIssue, setSelectedIssue] = useState<ApiCheckResult | null>(null);
  const [pendingCopy, setPendingCopy] = useState<{
    items: ApiCheckResult[];
    description: string;
  } | null>(null);
  const { getCheckById } = useDocumentationChecks();
  const documentationCheck = getCheckById(id);

  const toggle = () => onToggleOpen(id);
  const toggleShowAll = () => onToggleShowAll(id);

  const performCopy = (copyItems: ApiCheckResult[]) =>
    copyToClipboard(
      getGroupCopyText(copyItems, name),
      `Copied ${countWord(copyItems.length, 'issue')}.`
    );

  const triggerCopy = (copyItems: ApiCheckResult[], description: string) => {
    if (copyItems.length > LARGE_GROUP_THRESHOLD) {
      setPendingCopy({ items: copyItems, description });
    } else {
      performCopy(copyItems);
    }
  };

  const triggerCopyAll = () => triggerCopy(items, countWord(items.length, 'issue'));

  const triggerCopyIssue = () => {
    if (!selectedIssue) return;
    triggerCopy([selectedIssue], '1 issue');
  };

  const onCopyAllClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    triggerCopyAll();
  };

  const sameSeverityItems = useMemo(() => {
    if (!selectedIssue) return [];
    const level = normalizeLevel(selectedIssue.level);
    return items.filter((item) => normalizeLevel(item.level) === level);
  }, [items, selectedIssue]);

  const triggerCopySameSeverity = () => {
    if (!selectedIssue) return;
    const level = normalizeLevel(selectedIssue.level);
    triggerCopy(
      sameSeverityItems,
      countWord(sameSeverityItems.length, `${getLevelLabel(level)} issue`)
    );
  };

  return (
    <>
      <IssueGroupLayout
        id={`check-group-${id}`}
        name={name}
        level={highest}
        items={items}
        renderItem={(item, index) => (
          <IssueRow key={`${id}-${index}`} item={item} onOpen={() => setSelectedIssue(item)} />
        )}
        isOpen={isOpen}
        onToggleOpen={toggle}
        showAll={showAll}
        onToggleShowAll={toggleShowAll}
        actions={
          <Button
            size="compact-xs"
            variant="subtle"
            leftSection={<IconCopy size={13} />}
            onClick={onCopyAllClick}
          >
            Copy all ({items.length})
          </Button>
        }
      />

      <IssueDetailDrawer
        opened={selectedIssue !== null}
        onClose={() => setSelectedIssue(null)}
        issue={selectedIssue}
        checkName={name}
        documentationCheck={documentationCheck}
        onCopyIssue={triggerCopyIssue}
        groupCount={items.length}
        onCopyAll={triggerCopyAll}
        sameSeverityCount={sameSeverityItems.length}
        onCopySameSeverity={triggerCopySameSeverity}
      />

      <Modal
        opened={pendingCopy !== null}
        onClose={() => setPendingCopy(null)}
        title="Copy issues?"
        zIndex={Z_INDEX.modalAboveDrawer}
      >
        <Stack gap="md">
          <Text size="sm">
            This will copy {pendingCopy?.description} in &quot;{name}&quot; to your clipboard as a
            single block of text.
          </Text>
          <Group justify="flex-end">
            <Button variant="default" onClick={() => setPendingCopy(null)}>
              Cancel
            </Button>
            <Button
              onClick={() => {
                if (pendingCopy) performCopy(pendingCopy.items);
                setPendingCopy(null);
              }}
            >
              Copy
            </Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
};

export default React.memo(CheckGroup);
