import { Group, Paper, Stack, Table, Text, Title, useMantineTheme } from '@mantine/core';
import { useMemo } from 'react';
import ObjectsGameModeSelector from './ObjectsGameModeSelector.tsx';
import { ObjectTypeEntriesPopover } from './ObjectTypeEntriesPopover.tsx';
import AppTable, {
  DifficultyTableCell,
  DifficultyTableHeaderCell,
} from '../../../common/AppTable.tsx';
import GameModeIcon from '../../../icons/GameModeIcon.tsx';
import {
  getObjectTypeBuckets,
  OBJECT_PERCENTAGE_COLUMNS,
  resolveObjectPercentageEntries,
  resolveObjectPercentageValue,
} from '../objectPercentagesUtils.ts';
import type { Mode, ObjectsOverviewDifficulty } from '../../../../Types';
import type { ObjectPercentageColumn } from '../objectPercentagesUtils.ts';
import type { ObjectsModeGroup } from '../types.ts';

function PercentageTableValue({ count, percentage }: { count: number; percentage: number }) {
  return (
    <Stack gap={0} align="center">
      <Text size="sm" fw={600} c={count === 0 ? 'dimmed' : undefined}>
        {count.toLocaleString()}
      </Text>
      <Text size="xs" c="dimmed">
        {percentage.toFixed(1)}%
      </Text>
    </Stack>
  );
}

function ObjectPercentagesDifficultyRow({
  mode,
  difficulty,
  columns,
  clickableCellHoverColor,
}: {
  mode: Mode;
  difficulty: ObjectsOverviewDifficulty;
  columns: ObjectPercentageColumn[];
  clickableCellHoverColor: string;
}) {
  const buckets = useMemo(() => getObjectTypeBuckets(difficulty, mode), [difficulty, mode]);

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
      {columns.map((column) => {
        const value = resolveObjectPercentageValue(buckets, column);
        const entries = resolveObjectPercentageEntries(buckets, column);
        const isClickable = entries.length > 0;

        return (
          <Table.Td
            key={`${difficulty.version}-${column.label}`}
            style={{
              textAlign: 'center',
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
            <ObjectTypeEntriesPopover
              headingLabel={column.label}
              difficultyVersion={difficulty.version}
              entries={entries}
            >
              <PercentageTableValue count={value.count} percentage={value.percentage} />
            </ObjectTypeEntriesPopover>
          </Table.Td>
        );
      })}
    </Table.Tr>
  );
}

interface ObjectPercentagesOverviewProps {
  groupedDifficulties: ObjectsModeGroup[];
  selectedMode?: Mode;
  onModeChange: (mode: Mode) => void;
  difficulties: ObjectsOverviewDifficulty[];
}

export default function ObjectPercentagesOverview({
  groupedDifficulties,
  selectedMode,
  onModeChange,
  difficulties,
}: ObjectPercentagesOverviewProps) {
  const theme = useMantineTheme();
  const clickableCellHoverColor = theme.colors.dark[4];
  const activeMode = selectedMode ?? groupedDifficulties[0]?.mode;
  const columns = activeMode ? OBJECT_PERCENTAGE_COLUMNS[activeMode] : [];

  if (!activeMode || difficulties.length === 0) {
    return null;
  }

  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap="md">
        <Group justify="space-between" align="flex-start" wrap="wrap">
          <Stack gap={2}>
            <Title order={4}>Objects overview</Title>
            <Text size="sm" c="dimmed">
              Click on a cell to see all objects for that type.
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
              {columns.map((column) => (
                <Table.Th key={column.label} style={{ textAlign: 'center' }}>
                  {column.label}
                </Table.Th>
              ))}
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {difficulties.map((difficulty) => (
              <ObjectPercentagesDifficultyRow
                key={`${activeMode}-${difficulty.version}`}
                mode={activeMode}
                difficulty={difficulty}
                columns={columns}
                clickableCellHoverColor={clickableCellHoverColor}
              />
            ))}
          </Table.Tbody>
        </AppTable>
      </Stack>
    </Paper>
  );
}
