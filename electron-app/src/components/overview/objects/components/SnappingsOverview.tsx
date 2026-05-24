import { Badge, Group, Paper, Stack, Table, Text, Title, useMantineTheme } from '@mantine/core';
import { CSSProperties, useMemo } from 'react';
import { EdgeTimesPopover } from './EdgeTimesPopover.tsx';
import AppTable, {
  DifficultyTableCell,
  DifficultyTableHeaderCell,
} from '../../../common/AppTable.tsx';
import GameModeIcon from '../../../icons/GameModeIcon.tsx';
import ObjectsGameModeSelector from './ObjectsGameModeSelector.tsx';
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
              padding: isClickable ? 0 : undefined,
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
  selectedMode?: Mode;
  onModeChange: (mode: Mode) => void;
  difficulties: ObjectsOverviewDifficulty[];
}

export default function SnappingsOverview({
  groupedDifficulties,
  selectedMode,
  onModeChange,
  difficulties,
}: SnappingsOverviewProps) {
  const theme = useMantineTheme();
  const clickableCellHoverColor = theme.colors.dark[4];
  const activeMode = selectedMode ?? groupedDifficulties[0]?.mode;
  const snappingColumns = getSnappingColumns(difficulties);

  if (!activeMode || difficulties.length === 0) {
    return null;
  }

  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap="md">
        <Group justify="space-between" align="flex-start" wrap="wrap">
          <Stack gap={2}>
            <Title order={4}>Snapping overview</Title>
            <Text size="sm" c="dimmed">
              Click on a cell to see all timestamps for that snapping.
            </Text>
          </Stack>
          <ObjectsGameModeSelector
            groupedDifficulties={groupedDifficulties}
            selectedMode={activeMode}
            onModeChange={onModeChange}
          />
        </Group>
        <AppTable highlightOnHover={false}>
          <Table.Thead style={{ backgroundColor: theme.colors.dark[5] }}>
            <Table.Tr>
              <DifficultyTableHeaderCell>Difficulty</DifficultyTableHeaderCell>
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
            {difficulties.map((difficulty) => (
              <SnappingDifficultyTableRow
                key={`${activeMode}-${difficulty.version}`}
                mode={activeMode}
                difficulty={difficulty}
                snappingColumns={snappingColumns}
                clickableCellHoverColor={clickableCellHoverColor}
              />
            ))}
          </Table.Tbody>
        </AppTable>
      </Stack>
    </Paper>
  );
}
