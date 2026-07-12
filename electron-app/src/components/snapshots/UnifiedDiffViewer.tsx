import { Accordion, Badge, Group, Text, useMantineTheme } from '@mantine/core';
import { IconX } from '@tabler/icons-react';
import { MouseEvent, useMemo, useState } from 'react';
import SnapshotDiffLine, { getDiffTypeIcon } from './SnapshotDiffLine';
import { ApiSnapshotCommit, ApiSnapshotSection, DiffType } from '../../Types';
import { InfoIconTooltip } from '../common/InfoIconTooltip.tsx';
import VirtualizedList from '../common/VirtualizedList.tsx';

interface UnifiedDiffViewerProps {
  commit: ApiSnapshotCommit;
}

function SectionAccordion({ section }: { section: ApiSnapshotSection }) {
  const [activeDiffFilter, setActiveDiffFilter] = useState<DiffType | null>(null);
  // Diffs already arrive pre-sorted from the API (chronologically for timestamped
  // sections, by change type otherwise - see DiffTranslator.SortMode).
  const visibleDiffs = useMemo(
    () =>
      activeDiffFilter === null
        ? section.diffs
        : section.diffs.filter((diff) => diff.diffType === activeDiffFilter),
    [activeDiffFilter, section.diffs]
  );

  const handleBadgeClick = (event: MouseEvent<HTMLElement>, diffType: DiffType) => {
    event.preventDefault();
    event.stopPropagation();
    setActiveDiffFilter((current) => (current === diffType ? null : diffType));
  };

  const renderFilterBadge = (count: number, label: string, color: string, diffType: DiffType) => {
    if (count <= 0) {
      return null;
    }

    const isActive = activeDiffFilter === diffType;
    return (
      <Badge
        size="sm"
        color={color}
        variant={isActive ? 'filled' : 'light'}
        style={{ cursor: 'pointer', userSelect: 'none' }}
        onClick={(event) => handleBadgeClick(event, diffType)}
      >
        <Group gap={4} wrap="nowrap">
          <Text inherit>{count}</Text>
          <Text inherit>{label}</Text>
          {isActive && <IconX size={12} />}
        </Group>
      </Badge>
    );
  };

  return (
    <Accordion.Item value={section.name}>
      <Accordion.Control>
        <Group gap="sm">
          {getDiffTypeIcon(section.aggregatedDiffType, 18)}
          <Text fw={500}>{section.name}</Text>
          {renderFilterBadge(section.additions, 'Added', 'green', 'Added')}
          {renderFilterBadge(section.removals, 'Removed', 'red', 'Removed')}
          {renderFilterBadge(section.modifications, 'Changed', 'yellow', 'Changed')}
          <InfoIconTooltip label="Click badges to filter by diff type" />
        </Group>
      </Accordion.Control>
      <Accordion.Panel>
        <VirtualizedList
          items={visibleDiffs}
          estimateSize={() => 52}
          getItemKey={(diff, index) => `${section.name}-${index}-${diff.message}`}
          renderItem={(diff) => <SnapshotDiffLine diff={diff} />}
        />
      </Accordion.Panel>
    </Accordion.Item>
  );
}

function UnifiedDiffViewer({ commit }: UnifiedDiffViewerProps) {
  const theme = useMantineTheme();
  const [expandedSections, setExpandedSections] = useState<string[]>(
    commit.sections.map((section) => section.name)
  );
  const [prevCommit, setPrevCommit] = useState(commit);

  if (commit !== prevCommit) {
    setPrevCommit(commit);
    setExpandedSections(commit.sections.map((section) => section.name));
  }

  if (commit.sections.length === 0) {
    return (
      <Text c="dimmed" size="sm" py="md">
        This difficulty did not change while the mapset was snapshotted at this time.
      </Text>
    );
  }

  return (
    <Accordion
      variant="separated"
      multiple
      value={expandedSections}
      onChange={setExpandedSections}
      styles={{
        root: {
          backgroundColor: theme.colors.dark[6],
          border: 0,
        },
        item: {
          backgroundColor: theme.colors.dark[7],
          borderRadius: theme.radius.md,
          border: 0,
          boxShadow: 'none',
          overflow: 'hidden',
          '&[data-active]': {
            border: 0,
            boxShadow: 'none',
          },
          '&::before': {
            display: 'none',
          },
        },
        control: {
          borderRadius: theme.radius.md,
        },
      }}
    >
      {commit.sections.map((section) => (
        <SectionAccordion key={section.name} section={section} />
      ))}
    </Accordion>
  );
}

export default UnifiedDiffViewer;
