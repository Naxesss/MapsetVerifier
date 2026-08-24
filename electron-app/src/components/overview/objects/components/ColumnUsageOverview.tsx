import { Box, Group, Paper, Stack, Table, Text, Title, useMantineTheme } from '@mantine/core';
import ObjectsGameModeSelector from './ObjectsGameModeSelector.tsx';
import AppTable, {
  DifficultyTableCell,
  DifficultyTableHeaderCell,
} from '../../../common/AppTable.tsx';
import GameModeIcon from '../../../icons/GameModeIcon.tsx';
import type { Mode, ObjectsColumnUsage, ObjectsOverviewDifficulty } from '../../../../Types';
import type { ObjectsModeGroup } from '../types.ts';
import type { MantineTheme } from '@mantine/core';
import type { CSSProperties } from 'react';

/** Mirrors the thresholds of the mania "Column usage" check (CheckColumnDistribution). */
const WARNING_DEVIATION = 0.2;

type UsageStatus = 'unused' | 'warning' | 'ok';

const STATUS_COLORS: Record<Exclude<UsageStatus, 'ok'>, string> = {
  unused: 'red',
  warning: 'orange',
};

function getUsageStatus(count: number, average: number): UsageStatus {
  if (count === 0) {
    return 'unused';
  }

  return Math.abs(count / average - 1) >= WARNING_DEVIATION ? 'warning' : 'ok';
}

function usageCellStyle(theme: MantineTheme, status: UsageStatus): CSSProperties | undefined {
  if (status === 'ok') {
    return undefined;
  }

  const colorName = STATUS_COLORS[status];

  return {
    backgroundColor: `${theme.colors[colorName][9]}33`,
    boxShadow: `inset 0 -3px 0 ${theme.colors[colorName][5]}`,
  };
}
function ColumnUsageCell({
  usage,
  average,
  peak,
}: {
  usage: ObjectsColumnUsage;
  average: number;
  peak: number;
}) {
  const theme = useMantineTheme();
  const status = getUsageStatus(usage.totalCount, average);
  const barColor = status === 'ok' ? theme.colors.blue[5] : theme.colors[STATUS_COLORS[status]][5];

  return (
    <Table.Td style={{ textAlign: 'center', ...usageCellStyle(theme, status) }}>
      <Stack gap={2} align="center">
        <Text size="sm" fw={600} c={usage.totalCount === 0 ? 'dimmed' : undefined}>
          {usage.totalCount.toLocaleString()}
        </Text>
        <Text size="xs" c="dimmed">
          {usage.percentage.toFixed(1)}%
        </Text>
        <Box
          style={{
            width: 40,
            height: 4,
            borderRadius: 2,
            backgroundColor: theme.colors.dark[4],
            overflow: 'hidden',
          }}
        >
          <Box
            style={{
              width: `${peak === 0 ? 0 : (usage.totalCount / peak) * 100}%`,
              height: '100%',
              backgroundColor: barColor,
            }}
          />
        </Box>
      </Stack>
    </Table.Td>
  );
}

function LegendSwatch({ color, label }: { color: string; label: string }) {
  const theme = useMantineTheme();

  return (
    <Group gap={6} wrap="nowrap">
      <Box
        style={{
          width: 10,
          height: 10,
          borderRadius: 2,
          backgroundColor: theme.colors[color][5],
        }}
      />
      <Text size="xs" c="dimmed">
        {label}
      </Text>
    </Group>
  );
}

interface ColumnUsageOverviewProps {
  groupedDifficulties: ObjectsModeGroup[];
  selectedMode?: Mode;
  onModeChange: (mode: Mode) => void;
  difficulties: ObjectsOverviewDifficulty[];
}

export default function ColumnUsageOverview({
  groupedDifficulties,
  selectedMode,
  onModeChange,
  difficulties,
}: ColumnUsageOverviewProps) {
  const theme = useMantineTheme();
  const activeMode = selectedMode ?? groupedDifficulties[0]?.mode;

  const maniaDifficulties = difficulties.filter(
    (difficulty) => (difficulty.columnUsage?.length ?? 0) > 0
  );

  if (activeMode !== 'Mania' || maniaDifficulties.length === 0) {
    return null;
  }

  const maxColumnCount = Math.max(
    ...maniaDifficulties.map((difficulty) => difficulty.columnUsage?.length ?? 0)
  );

  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap="md">
        <Group justify="space-between" align="flex-start" wrap="wrap">
          <Stack gap={2}>
            <Title order={4}>Column usage</Title>
            <Text size="sm" c="dimmed">
              Objects per column with their share of the total. Hover a cell for its note and hold
              note split.
            </Text>
          </Stack>
          <ObjectsGameModeSelector
            groupedDifficulties={groupedDifficulties}
            selectedMode={activeMode}
            onModeChange={onModeChange}
          />
        </Group>

        <Group gap="md">
          <LegendSwatch color="blue" label="Evenly used" />
          <LegendSwatch
            color="orange"
            label={`Over/underused (${WARNING_DEVIATION * 100}% off average)`}
          />
          <LegendSwatch color="red" label="Unused column" />
        </Group>

        <AppTable highlightOnHover={false}>
          <Table.Thead style={{ backgroundColor: theme.colors.dark[5] }}>
            <Table.Tr>
              <DifficultyTableHeaderCell>Difficulty</DifficultyTableHeaderCell>
              <Table.Th style={{ textAlign: 'center' }}>Keys</Table.Th>
              <Table.Th style={{ textAlign: 'center' }}>Total</Table.Th>
              {Array.from({ length: maxColumnCount }, (_, index) => (
                <Table.Th key={index} style={{ textAlign: 'center' }}>
                  {index + 1}
                </Table.Th>
              ))}
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {maniaDifficulties.map((difficulty) => {
              const columnUsage = difficulty.columnUsage ?? [];
              const total = columnUsage.reduce((sum, usage) => sum + usage.totalCount, 0);
              const average = total / columnUsage.length;
              const peak = Math.max(...columnUsage.map((usage) => usage.totalCount));

              return (
                <Table.Tr key={difficulty.version}>
                  <DifficultyTableCell>
                    <Group gap="xs" wrap="nowrap">
                      <GameModeIcon
                        mode={activeMode}
                        size={16}
                        starRating={difficulty.starRating}
                      />
                      <Text size="sm" fw={600}>
                        {difficulty.version}
                      </Text>
                    </Group>
                  </DifficultyTableCell>
                  <Table.Td style={{ textAlign: 'center' }}>
                    <Text size="sm">{columnUsage.length}K</Text>
                  </Table.Td>
                  <Table.Td style={{ textAlign: 'center' }}>
                    <Text size="sm">{total.toLocaleString()}</Text>
                  </Table.Td>
                  {Array.from({ length: maxColumnCount }, (_, index) =>
                    index < columnUsage.length ? (
                      <ColumnUsageCell
                        key={index}
                        usage={columnUsage[index]}
                        average={average}
                        peak={peak}
                      />
                    ) : (
                      <Table.Td key={index} style={{ textAlign: 'center' }}>
                        <Text size="sm" c="dimmed">
                          -
                        </Text>
                      </Table.Td>
                    )
                  )}
                </Table.Tr>
              );
            })}
          </Table.Tbody>
        </AppTable>
      </Stack>
    </Paper>
  );
}
