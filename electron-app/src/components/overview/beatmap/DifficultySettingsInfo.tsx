import { Group, Paper, Stack, Table, Text, useMantineTheme } from '@mantine/core';
import { formatGameModeLabel, getModeAccentColor } from '../../../utils/gameMode';
import AppTable, {
  DifficultyTableCell,
  DifficultyTableHeaderCell,
} from '../../common/AppTable.tsx';
import GameModeIcon from '../../icons/GameModeIcon.tsx';
import type { DifficultyDifficultySettings } from '../../../Types';

interface DifficultySettingsInfoProps {
  difficultySettings: DifficultyDifficultySettings[];
}

function formatNullable(value: string | number | null, fallback = 'N/A') {
  if (value === null || value === '') {
    return fallback;
  }

  return value;
}

function formatDifficultyValue(value: number) {
  return value.toFixed(1);
}

function ModeCell({ mode }: { mode: string }) {
  return (
    <Group gap={6} wrap="nowrap" justify="center">
      <GameModeIcon mode={mode} size={16} color={getModeAccentColor(mode)} />
      <Text size="sm">{formatGameModeLabel(mode)}</Text>
    </Group>
  );
}

function CircleSizeCell({ settings }: { settings: DifficultyDifficultySettings }) {
  const isTaiko = settings.mode === 'Taiko';

  if (isTaiko) {
    return <Text size="sm">N/A</Text>;
  }

  return <Text size="sm">{formatNullable(settings.circleSize)}</Text>;
}

function DifficultySettingsInfo({ difficultySettings }: DifficultySettingsInfoProps) {
  const theme = useMantineTheme();

  if (difficultySettings.length === 0) {
    return null;
  }

  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap="md">
        <Text fw={600}>Difficulty Settings</Text>

        <AppTable>
          <Table.Thead style={{ backgroundColor: theme.colors.dark[5] }}>
            <Table.Tr>
              <DifficultyTableHeaderCell>Difficulty</DifficultyTableHeaderCell>
              <Table.Th>Mode</Table.Th>
              <Table.Th>HP Drain</Table.Th>
              <Table.Th>Circle Size</Table.Th>
              <Table.Th>Overall Difficulty</Table.Th>
              <Table.Th>Approach Rate</Table.Th>
              <Table.Th>Slider Tick Rate</Table.Th>
              <Table.Th>Slider Velocity</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {difficultySettings.map((settings) => {
              const isMania = settings.mode === 'Mania';

              return (
                <Table.Tr key={`${settings.mode}-${settings.version}`}>
                  <DifficultyTableCell>
                    <Text size="sm" fw={600} style={{ whiteSpace: 'nowrap' }}>
                      {settings.version}
                    </Text>
                  </DifficultyTableCell>
                  <Table.Td>
                    <ModeCell mode={settings.mode} />
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm">{formatDifficultyValue(settings.hpDrain)}</Text>
                  </Table.Td>
                  <Table.Td>
                    <CircleSizeCell settings={settings} />
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm">{formatDifficultyValue(settings.overallDifficulty)}</Text>
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm">{isMania ? 'N/A' : formatNullable(settings.approachRate)}</Text>
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm">
                      {isMania ? 'N/A' : formatNullable(settings.sliderTickRate)}
                    </Text>
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm">
                      {isMania
                        ? 'N/A'
                        : settings.sliderVelocity
                          ? `${settings.sliderVelocity}x`
                          : 'N/A'}
                    </Text>
                  </Table.Td>
                </Table.Tr>
              );
            })}
          </Table.Tbody>
        </AppTable>
      </Stack>
    </Paper>
  );
}

export default DifficultySettingsInfo;
