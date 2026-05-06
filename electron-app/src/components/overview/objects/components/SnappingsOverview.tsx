import { Badge, Group, Paper, Stack, Table, Text, Title, useMantineTheme } from '@mantine/core';
import { Fragment } from 'react';
import { formatGameModeLabel, getModeAccentColor } from '../../../../utils/gameMode';
import AppTable, {
  DifficultyTableCell,
  DifficultyTableHeaderCell,
} from '../../../common/AppTable.tsx';
import GameModeIcon from '../../../icons/GameModeIcon.tsx';
import { getSnappingColumns } from '../timelineUtils.ts';
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

function SnappingStatusBadge({ count, percentage }: { count: number; percentage: number }) {
  return (
    <Badge color={count > 0 ? 'yellow' : 'green'} variant="light">
      {count.toLocaleString()} ({percentage.toFixed(1)}%)
    </Badge>
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
              Counts are based on object edge times, including slider reverses and tails.
            </Text>
          </Stack>
          <Badge color={totalUnsnappedCount > 0 ? 'yellow' : 'green'} variant="light">
            Unsnapped: {totalUnsnappedCount.toLocaleString()} ({totalUnsnappedPercentage.toFixed(1)}
            %)
          </Badge>
        </Group>
        <AppTable>
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
                  <Table.Tr key={`${group.mode}-${difficulty.version}`}>
                    <DifficultyTableCell>
                      <Group gap="xs" wrap="nowrap">
                        <GameModeIcon
                          mode={group.mode}
                          size={16}
                          starRating={difficulty.starRating}
                        />
                        <Text size="sm" fw={600}>
                          {difficulty.version}
                        </Text>
                      </Group>
                    </DifficultyTableCell>
                    <Table.Td>
                      <Group gap={6} wrap="nowrap" justify="center">
                        <GameModeIcon
                          mode={group.mode}
                          size={16}
                          color={getModeAccentColor(group.mode)}
                        />
                        <Text size="sm">{formatGameModeLabel(group.mode)}</Text>
                      </Group>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{difficulty.objectCount.toLocaleString()}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{difficulty.edgeCount.toLocaleString()}</Text>
                    </Table.Td>
                    {snappingColumns.map((column) => {
                      const bucket = difficulty.snappings.find(
                        (candidate) => candidate.label === column.label
                      );
                      return (
                        <Table.Td key={`${difficulty.mode}-${difficulty.version}-${column.label}`}>
                          <SnappingTableValue
                            count={bucket?.count ?? 0}
                            percentage={bucket?.percentage ?? 0}
                          />
                        </Table.Td>
                      );
                    })}
                    <Table.Td>
                      <Group justify="center" wrap="nowrap">
                        <SnappingStatusBadge
                          count={difficulty.unsnappedCount}
                          percentage={difficulty.unsnappedPercentage}
                        />
                      </Group>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Fragment>
            ))}
          </Table.Tbody>
        </AppTable>
      </Stack>
    </Paper>
  );
}
