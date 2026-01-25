import { Text, Badge, Group, Paper, useMantineTheme, Stack, Box, SimpleGrid } from '@mantine/core';
import { DifficultyStatistics } from '../../../Types';
import { groupByValue, ValueGroup } from './utils/groupByValue';

interface StatisticsInfoProps {
  statistics: DifficultyStatistics[];
}

function getStatisticsKey(stats: DifficultyStatistics): string {
  const parts = [
    stats.circleCount,
    stats.sliderCount ?? 'na',
    stats.spinnerCount ?? 'na',
    stats.holdNoteCount ?? 'na',
    stats.objectsPerColumn?.join(',') ?? 'na',
    stats.newComboCount,
    stats.breakCount,
    stats.uninheritedLineCount,
    stats.inheritedLineCount,
  ];
  return parts.join('|');
}

function StatRow({ label, value }: { label: string; value: string | number | null }) {
  if (value === null) return null;
  return (
    <Group justify="space-between" gap="xs">
      <Text size="xs" c="dimmed">{label}</Text>
      <Text size="xs" fw={500}>{value}</Text>
    </Group>
  );
}

function StatisticsGroup({ group }: { group: ValueGroup<DifficultyStatistics> }) {
  const theme = useMantineTheme();
  const stats = group.value;
  const isMania = stats.mode === 'Mania';

  return (
    <Box
      p="sm"
      style={{
        backgroundColor: theme.colors.dark[6],
        borderRadius: theme.radius.sm,
      }}
    >
      <Group gap="xs" mb="xs" wrap="wrap">
        {group.difficulties.map((diff, idx) => (
          <Badge key={idx} size="xs" variant="light">
            {diff}
          </Badge>
        ))}
        <Badge size="xs" variant="outline" color="gray">
          {stats.mode}
        </Badge>
      </Group>

      <SimpleGrid cols={2} spacing="xs">
        <Stack gap={4}>
          <StatRow label="Circles" value={stats.circleCount} />
          {!isMania && <StatRow label="Sliders" value={stats.sliderCount} />}
          {!isMania && <StatRow label="Spinners" value={stats.spinnerCount} />}
          {isMania && <StatRow label="Hold Notes" value={stats.holdNoteCount} />}
          <StatRow label="New Combos" value={stats.newComboCount} />
          <StatRow label="Breaks" value={stats.breakCount} />
        </Stack>
        <Stack gap={4}>
          <StatRow label="Uninherited Lines" value={stats.uninheritedLineCount} />
          <StatRow label="Inherited Lines" value={stats.inheritedLineCount} />
          <StatRow label="Kiai Time" value={stats.kiaiTimeFormatted} />
          <StatRow label="Drain Time" value={stats.drainTimeFormatted} />
          <StatRow label="Play Time" value={stats.playTimeFormatted} />
          {stats.starRating !== null && (
            <StatRow label="Star Rating" value={`${stats.starRating.toFixed(2)}★`} />
          )}
        </Stack>
      </SimpleGrid>

      {isMania && stats.objectsPerColumn && (
        <Box mt="xs">
          <Text size="xs" c="dimmed" mb={4}>Objects per Column</Text>
          <Group gap="xs">
            {stats.objectsPerColumn.map((count, idx) => (
              <Badge key={idx} size="xs" variant="light" color="blue">
                {idx + 1}: {count}
              </Badge>
            ))}
          </Group>
        </Box>
      )}
    </Box>
  );
}

function StatisticsInfo({ statistics }: StatisticsInfoProps) {
  const theme = useMantineTheme();

  if (statistics.length === 0) {
    return null;
  }

  const groups = groupByValue(statistics, getStatisticsKey);

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
      <Text fw={600} mb="md">Statistics</Text>
      <Stack gap="sm">
        {groups.map((group) => (
          <StatisticsGroup key={group.key} group={group} />
        ))}
      </Stack>
    </Paper>
  );
}

export default StatisticsInfo;

