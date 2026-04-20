import { Group, Paper, Stack, Table, Text, useMantineTheme } from '@mantine/core';
import { formatGameModeLabel } from '../../../utils/gameMode';
import AppTable, { DifficultyTableCell, DifficultyTableHeaderCell } from '../../common/AppTable.tsx';
import GameModeIcon from '../../icons/GameModeIcon.tsx';
import type { DifficultyStatistics } from '../../../Types';

interface StatisticsInfoProps {
  statistics: DifficultyStatistics[];
}

function getModeAccentColor(mode: string, theme: ReturnType<typeof useMantineTheme>) {
  switch (mode) {
    case 'Standard':
      return theme.colors.pink[4];
    case 'Taiko':
      return theme.colors.red[4];
    case 'Catch':
      return theme.colors.lime[4];
    case 'Mania':
      return theme.colors.violet[4];
    default:
      return theme.colors.gray[4];
  }
}

function formatCount(value: number | null) {
  return value === null ? 'N/A' : value.toLocaleString();
}

function formatStarRating(starRating: number | null) {
  return starRating === null ? 'N/A' : `${starRating.toFixed(2)}★`;
}

function ModeCell({ mode }: { mode: string }) {
  const theme = useMantineTheme();

  return (
    <Group gap={6} wrap="nowrap" justify="center">
      <GameModeIcon mode={mode} size={16} color={getModeAccentColor(mode, theme)} />
      <Text size="sm">{formatGameModeLabel(mode)}</Text>
    </Group>
  );
}

function SliderCell({ stats }: { stats: DifficultyStatistics }) {
  const isMania = stats.mode === 'Mania';
  const value = isMania ? stats.holdNoteCount : stats.sliderCount;

  return (
    <Stack gap={4}>
      {<Text size="sm" fw={500}>{isMania ? "N/A" : formatCount(value)}</Text>}
    </Stack>
  );
}

function StatisticsInfo({ statistics }: StatisticsInfoProps) {
  const theme = useMantineTheme();

  if (statistics.length === 0) {
    return null;
  }

  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap="md">
        <Text fw={600}>Statistics</Text>
        <AppTable>
          <Table.Thead style={{ backgroundColor: theme.colors.dark[5] }}>
            <Table.Tr>
              <DifficultyTableHeaderCell rowSpan={2}>Difficulty</DifficultyTableHeaderCell>
              <Table.Th rowSpan={2}>Mode</Table.Th>
              <Table.Th rowSpan={2} >Star Rating</Table.Th>
              <Table.Th colSpan={3}>Objects</Table.Th>
              <Table.Th colSpan={2}>Misc</Table.Th>
              <Table.Th colSpan={2}>Timing</Table.Th>
              <Table.Th colSpan={2}>Duration</Table.Th>
            </Table.Tr>
            <Table.Tr>
              <Table.Th>Circles</Table.Th>
              <Table.Th>Sliders</Table.Th>
              <Table.Th>Spinners</Table.Th>
              <Table.Th>New Combos</Table.Th>
              <Table.Th>Breaks</Table.Th>
              <Table.Th>Uninherited</Table.Th>
              <Table.Th>Inherited</Table.Th>
              <Table.Th>Drain Time</Table.Th>
              <Table.Th>Play Time</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {statistics.map((stats) => (
              <Table.Tr key={`${stats.mode}-${stats.version}`}>
                <DifficultyTableCell>
                  <Text size="sm" fw={600} style={{ whiteSpace: 'nowrap' }}>{stats.version}</Text>
                </DifficultyTableCell>
                <Table.Td>
                  <ModeCell mode={stats.mode} />
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{formatStarRating(stats.starRating)}</Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{stats.circleCount.toLocaleString()}</Text>
                </Table.Td>
                <Table.Td>
                  <SliderCell stats={stats} />
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{formatCount(stats.spinnerCount)}</Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{stats.newComboCount.toLocaleString()}</Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{stats.breakCount.toLocaleString()}</Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{stats.uninheritedLineCount.toLocaleString()}</Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{stats.inheritedLineCount.toLocaleString()}</Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{stats.drainTimeFormatted}</Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{stats.playTimeFormatted}</Text>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </AppTable>
      </Stack>
    </Paper>
  );
}

export default StatisticsInfo;

