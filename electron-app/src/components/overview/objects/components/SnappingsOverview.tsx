import { Badge, Group, Paper, Stack, Table, Text, Title, useMantineTheme } from '@mantine/core';
import { CSSProperties, Fragment, useMemo } from 'react';
import { EdgeTimesPopover } from './EdgeTimesPopover.tsx';
import { formatGameModeLabel, getModeAccentColor } from '../../../../utils/gameMode';
import AppTable, {
  DifficultyTableCell,
  DifficultyTableHeaderCell,
} from '../../../common/AppTable.tsx';
import GameModeIcon from '../../../icons/GameModeIcon.tsx';
import { buildRoundedEdgePartNameMap, getSnappingColumns } from '../timelineUtils.ts';
import type { Mode, ObjectsOverviewDifficulty, ObjectsSnappingBucket } from '../../../../Types';
import type { ObjectsModeGroup } from '../types.ts';

function SnappingTableValue({ count, percentage }: { count: number; percentage: number }) {
  return (
    <Stack gap={0}>
      <Text size="sm" fw={600} c={count === 0 ? 'dimmed' : undefined}>
        {count.toLocaleString()}
      </Text>
      <Text size="xs" c="dimmed">
        {percentage.toFixed(1)}%
      </Text>
    </Stack>
  );
}

function SnappingStatusBadge({
  count,
  percentage,
  style,
}: {
  count: number;
  percentage: number;
  style?: CSSProperties;
}) {
  return (
    <Badge color={count > 0 ? 'yellow' : 'green'} variant="light" style={style}>
      {count.toLocaleString()} ({percentage.toFixed(1)}%)
    </Badge>
  );
}

function SnappingDifficultyTableRow({
  mode,
  difficulty,
  snappingColumns,
  clickableCellHoverColor,
}: {
  mode: Mode;
  difficulty: ObjectsOverviewDifficulty;
  snappingColumns: ObjectsSnappingBucket[];
  clickableCellHoverColor: string;
}) {
  const roundedEdgePartNameMap = useMemo(
    () => buildRoundedEdgePartNameMap(difficulty),
    [difficulty]
  );

  return (
    <Table.Tr>
      <DifficultyTableCell>
        <Group gap="xs" wrap="nowrap">
          <GameModeIcon mode={mode} size={16} starRating={difficulty.starRating} />
          <Text size="sm" fw={600}>
            {difficulty.version}
          </Text>
        </Group>
      </DifficultyTableCell>
      <Table.Td>
        <Group gap={6} wrap="nowrap" justify="center">
          <GameModeIcon mode={mode} size={16} color={getModeAccentColor(mode)} />
          <Text size="sm">{formatGameModeLabel(mode)}</Text>
        </Group>
      </Table.Td>
      <Table.Td>
        <Text size="sm">{difficulty.objectCount.toLocaleString()}</Text>
      </Table.Td>
      <Table.Td>
        <Text size="sm">{difficulty.edgeCount.toLocaleString()}</Text>
      </Table.Td>
      {snappingColumns.map((column) => {
        const bucket = difficulty.snappings.find((candidate) => candidate.label === column.label);
        const timesMs = bucket?.edgeTimesMs ?? [];
        const count = bucket?.count ?? 0;
        const isClickable = timesMs.length > 0;
        return (
          <Table.Td
            key={`${difficulty.mode}-${difficulty.version}-${column.label}`}
            style={{
              cursor: isClickable ? 'pointer' : undefined,
              transition: isClickable ? 'background-color 120ms' : undefined,
            }}
            onMouseEnter={
              isClickable
                ? (event) => {
                    event.currentTarget.style.backgroundColor = clickableCellHoverColor;
                  }
                : undefined
            }
            onMouseLeave={
              isClickable
                ? (event) => {
                    event.currentTarget.style.backgroundColor = '';
                  }
                : undefined
            }
          >
            <EdgeTimesPopover
              headingLabel={`${column.label} snaps`}
              difficultyVersion={difficulty.version}
              timesMs={timesMs}
              roundedEdgePartNameMap={roundedEdgePartNameMap}
            >
              <SnappingTableValue count={count} percentage={bucket?.percentage ?? 0} />
            </EdgeTimesPopover>
          </Table.Td>
        );
      })}
      <Table.Td
        style={{
          cursor: (difficulty.unsnappedEdgeTimesMs?.length ?? 0) > 0 ? 'pointer' : undefined,
        }}
      >
        <Group justify="center" wrap="nowrap">
          <EdgeTimesPopover
            headingLabel="Unsnapped objects"
            difficultyVersion={difficulty.version}
            timesMs={difficulty.unsnappedEdgeTimesMs ?? []}
            fullWidth={false}
            hoverHighlightColor={clickableCellHoverColor}
            roundedEdgePartNameMap={roundedEdgePartNameMap}
          >
            <SnappingStatusBadge
              count={difficulty.unsnappedCount}
              percentage={difficulty.unsnappedPercentage}
              style={{
                cursor:
                  (difficulty.unsnappedEdgeTimesMs?.length ?? 0) > 0 ? 'pointer' : undefined,
              }}
            />
          </EdgeTimesPopover>
        </Group>
      </Table.Td>
    </Table.Tr>
  );
}

interface SnappingsOverviewProps {
  groupedDifficulties: ObjectsModeGroup[];
  totalUnsnappedCount: number;
  totalEdgeCount: number;
}

export default function SnappingsOverview({
  groupedDifficulties,
  totalUnsnappedCount,
  totalEdgeCount,
}: SnappingsOverviewProps) {
  const theme = useMantineTheme();
  const clickableCellHoverColor = theme.colors.dark[4];
  const totalUnsnappedPercentage =
    totalEdgeCount > 0 ? (totalUnsnappedCount * 100) / totalEdgeCount : 0;
  const difficulties = groupedDifficulties.flatMap((group) => group.difficulties);
  const snappingColumns = getSnappingColumns(difficulties);

  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap="md">
        <Group justify="space-between" align="flex-start">
          <Stack gap={2}>
            <Title order={4}>Snapping overview</Title>
            <Text size="sm" c="dimmed">
              Click on a cell to see all timestamps for that snapping.
            </Text>
          </Stack>
          <Badge color={totalUnsnappedCount > 0 ? 'yellow' : 'green'} variant="light">
            Unsnapped: {totalUnsnappedCount.toLocaleString()} ({totalUnsnappedPercentage.toFixed(1)}
            %)
          </Badge>
        </Group>
        <AppTable highlightOnHover={false}>
          <Table.Thead style={{ backgroundColor: theme.colors.dark[5] }}>
            <Table.Tr>
              <DifficultyTableHeaderCell>Difficulty</DifficultyTableHeaderCell>
              <Table.Th>Mode</Table.Th>
              <Table.Th style={{ textAlign: 'center' }}>Objects</Table.Th>
              <Table.Th style={{ textAlign: 'center' }}>Edges</Table.Th>
              {snappingColumns.map((column) => (
                <Table.Th key={column.label} style={{ textAlign: 'center' }}>
                  {column.label}
                </Table.Th>
              ))}
              <Table.Th style={{ textAlign: 'center' }}>Unsnapped</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {groupedDifficulties.map((group) => (
              <Fragment key={group.mode}>
                {group.difficulties.map((difficulty) => (
                  <SnappingDifficultyTableRow
                    key={`${group.mode}-${difficulty.version}`}
                    mode={group.mode}
                    difficulty={difficulty}
                    snappingColumns={snappingColumns}
                    clickableCellHoverColor={clickableCellHoverColor}
                  />
                ))}
              </Fragment>
            ))}
          </Table.Tbody>
        </AppTable>
      </Stack>
    </Paper>
  );
}
