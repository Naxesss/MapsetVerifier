import { Text, Badge, Group, Paper, useMantineTheme, Stack, Box, Progress } from '@mantine/core';
import { DifficultyDifficultySettings } from '../../../Types';
import { groupByValue, ValueGroup } from './utils/groupByValue';

interface DifficultySettingsInfoProps {
  difficultySettings: DifficultyDifficultySettings[];
}

function getDifficultySettingsKey(settings: DifficultyDifficultySettings): string {
  const parts = [
    settings.hpDrain,
    settings.circleSize ?? 'na',
    settings.overallDifficulty,
    settings.approachRate ?? 'na',
    settings.sliderTickRate ?? 'na',
    settings.sliderVelocity ?? 'na',
  ];
  return parts.join('|');
}

function DifficultyBar({ label, value, max = 10 }: { label: string; value: number | string | null; max?: number }) {
  const theme = useMantineTheme();
  
  if (value === null) {
    return (
      <Group justify="space-between" gap="xs">
        <Text size="xs" c="dimmed">{label}</Text>
        <Text size="xs" c="dimmed">N/A</Text>
      </Group>
    );
  }

  const numValue = typeof value === 'string' ? parseFloat(value) : value;
  const percentage = Math.min((numValue / max) * 100, 100);
  const displayValue = typeof value === 'string' ? value : value.toFixed(1);

  return (
    <Box>
      <Group justify="space-between" gap="xs" mb={2}>
        <Text size="xs" c="dimmed">{label}</Text>
        <Text size="xs" fw={500}>{displayValue}</Text>
      </Group>
      <Progress 
        value={percentage} 
        size="xs" 
        color={percentage > 80 ? 'red' : percentage > 60 ? 'yellow' : 'blue'}
        bg={theme.colors.dark[4]}
      />
    </Box>
  );
}

function DifficultySettingsGroup({ group }: { group: ValueGroup<DifficultyDifficultySettings> }) {
  const theme = useMantineTheme();
  const settings = group.value;
  const isMania = settings.mode === 'Mania';
  const isTaiko = settings.mode === 'Taiko';

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
          {settings.mode}
        </Badge>
      </Group>

      <Stack gap="xs">
        <DifficultyBar label="HP Drain" value={settings.hpDrain} />
        {!isTaiko && (
          <DifficultyBar 
            label={isMania ? 'Key Count' : 'Circle Size'} 
            value={settings.circleSize} 
            max={isMania ? 10 : 10}
          />
        )}
        <DifficultyBar label="Overall Difficulty" value={settings.overallDifficulty} />
        {!isMania && (
          <DifficultyBar label="Approach Rate" value={settings.approachRate} />
        )}
        {!isMania && (
          <Group justify="space-between" gap="xs">
            <Text size="xs" c="dimmed">Slider Tick Rate</Text>
            <Text size="xs" fw={500}>{settings.sliderTickRate ?? 'N/A'}</Text>
          </Group>
        )}
        {!isMania && (
          <Group justify="space-between" gap="xs">
            <Text size="xs" c="dimmed">Slider Velocity</Text>
            <Text size="xs" fw={500}>{settings.sliderVelocity ?? 'N/A'}x</Text>
          </Group>
        )}
      </Stack>
    </Box>
  );
}

function DifficultySettingsInfo({ difficultySettings }: DifficultySettingsInfoProps) {
  const theme = useMantineTheme();

  if (difficultySettings.length === 0) {
    return null;
  }

  const groups = groupByValue(difficultySettings, getDifficultySettingsKey);

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
      <Text fw={600} mb="md">Difficulty Settings</Text>
      <Stack gap="sm">
        {groups.map((group) => (
          <DifficultySettingsGroup key={group.key} group={group} />
        ))}
      </Stack>
    </Paper>
  );
}

export default DifficultySettingsInfo;

