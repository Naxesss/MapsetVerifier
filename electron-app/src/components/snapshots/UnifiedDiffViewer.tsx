import { Accordion, Group, Text, useMantineTheme } from '@mantine/core';
import { IconEqual } from '@tabler/icons-react';
import { useMemo, useState } from 'react';
import SnapshotDiffLine, { getDiffTypeIcon } from './SnapshotDiffLine';
import { ApiSnapshotCommit, ApiSnapshotSection, DiffType } from '../../Types';
import EmptyState from '../common/EmptyState.tsx';
import FilterChip from '../common/FilterChip.tsx';
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

  const toggleFilter = (diffType: DiffType) =>
    setActiveDiffFilter((current) => (current === diffType ? null : diffType));

  const filterChip = (count: number, color: string, diffType: DiffType) =>
    count > 0 && (
      <FilterChip
        label={diffType}
        count={count}
        color={color}
        icon={getDiffTypeIcon(diffType, 14)}
        active={activeDiffFilter === diffType}
        onClick={() => toggleFilter(diffType)}
      />
    );

  return (
    <Accordion.Item value={section.name}>
      {/* The filters sit beside the control rather than in it: buttons can't nest in its button. */}
      <Group gap="xs" wrap="nowrap" pr="sm">
        <Accordion.Control style={{ flex: 1, minWidth: 0 }}>
          <Group gap="sm" wrap="nowrap">
            {getDiffTypeIcon(section.aggregatedDiffType, 18)}
            <Text size="sm" fw={600}>
              {section.name}
            </Text>
          </Group>
        </Accordion.Control>
        <Group gap="xs" wrap="nowrap" style={{ flexShrink: 0 }}>
          {filterChip(section.additions, 'green', 'Added')}
          {filterChip(section.removals, 'red', 'Removed')}
          {filterChip(section.modifications, 'yellow', 'Changed')}
        </Group>
      </Group>
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
      <EmptyState
        icon={IconEqual}
        title="No changes"
        description="This difficulty did not change in this snapshot."
      />
    );
  }

  return (
    <Accordion
      variant="separated"
      multiple
      chevronPosition="left"
      value={expandedSections}
      onChange={setExpandedSections}
      styles={{
        // Sections sit inside the Changes card, so they are a step lighter than it.
        root: {
          border: 0,
        },
        item: {
          backgroundColor: theme.colors.dark[6],
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
