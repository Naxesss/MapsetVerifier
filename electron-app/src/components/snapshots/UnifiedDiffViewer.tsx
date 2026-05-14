import {
  Accordion,
  Badge,
  Box,
  Group,
  Stack,
  Text,
  Tooltip,
  useMantineTheme,
} from '@mantine/core';
import { IconX, IconInfoCircle } from '@tabler/icons-react';
import { MouseEvent, useEffect, useState } from 'react';
import SnapshotDiffLine, { getDiffTypeIcon } from './SnapshotDiffLine';
import { ApiSnapshotCommit, ApiSnapshotSection, ApiSnapshotDiff, DiffType } from '../../Types';

interface UnifiedDiffViewerProps {
  commit: ApiSnapshotCommit;
}

function parseDiffTimestampMs(message: string): number | null {
  const normalTimestampMatch = message.match(/^(\d+):(\d{2}):(\d{3})\s-\s/);
  if (normalTimestampMatch) {
    const minutes = Number.parseInt(normalTimestampMatch[1], 10);
    const seconds = Number.parseInt(normalTimestampMatch[2], 10);
    const milliseconds = Number.parseInt(normalTimestampMatch[3], 10);
    return minutes * 60_000 + seconds * 1_000 + milliseconds;
  }

  const negativeTimestampMatch = message.match(/^(-\d+(?:\.\d+)?)\s-\s/);
  if (negativeTimestampMatch) {
    return Number.parseFloat(negativeTimestampMatch[1]);
  }

  return null;
}

function sortDiffsChronologically(diffs: ApiSnapshotDiff[]): ApiSnapshotDiff[] {
  return diffs
    .map((diff, index) => ({
      diff,
      index,
      timestampMs: parseDiffTimestampMs(diff.message),
    }))
    .sort((a, b) => {
      if (a.timestampMs === null || b.timestampMs === null) {
        return a.index - b.index;
      }

      if (a.timestampMs === b.timestampMs) {
        return a.index - b.index;
      }

      return a.timestampMs - b.timestampMs;
    })
    .map((entry) => entry.diff);
}

function SectionAccordion({ section }: { section: ApiSnapshotSection }) {
  const [activeDiffFilter, setActiveDiffFilter] = useState<DiffType | null>(null);
  const sortedDiffs = sortDiffsChronologically(section.diffs);

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
          <Tooltip label="Click badges to filter by diff type">
            <Box
              component="span"
              style={{ display: 'inline-flex', alignItems: 'center', cursor: 'help' }}
            >
              <IconInfoCircle size={16} color="var(--mantine-color-gray-6)" />
            </Box>
          </Tooltip>
        </Group>
      </Accordion.Control>
      <Accordion.Panel>
        <Stack gap="xs">
          {sortedDiffs.map(
            (diff, index) =>
              (activeDiffFilter === null || diff.diffType === activeDiffFilter) && (
                <SnapshotDiffLine key={`snapshot-${section.name}-${index}`} diff={diff} />
              )
          )}
        </Stack>
      </Accordion.Panel>
    </Accordion.Item>
  );
}

function UnifiedDiffViewer({ commit }: UnifiedDiffViewerProps) {
  const theme = useMantineTheme();
  const [expandedSections, setExpandedSections] = useState<string[]>(
    commit.sections.map((section) => section.name)
  );

  useEffect(() => {
    setExpandedSections(commit.sections.map((section) => section.name));
  }, [commit]);

  if (commit.sections.length === 0) {
    return (
      <Text c="dimmed" size="sm" ta="center" py="md">
        No changes in this commit.
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
