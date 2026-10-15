import { Badge, Group, Paper, Stack, Table, Text, Tooltip, useMantineTheme } from '@mantine/core';
import { IconAlertTriangle } from '@tabler/icons-react';
import { useGroupCellStyle } from './utils/useGroupCellStyle';
import { countWord } from '../../../utils/countWord.ts';
import { formatNullable } from '../../../utils/formatters';
import { formatGameModeLabel, getModeAccentColor } from '../../../utils/gameMode';
import { itemKey } from '../../../utils/inconsistencies';
import AppTable, {
  DifficultyTableCell,
  DifficultyTableHeaderCell,
} from '../../common/AppTable.tsx';
import OsuLink from '../../common/OsuLink.tsx';
import GameModeIcon from '../../icons/GameModeIcon.tsx';
import type { DifficultyGeneralSettings } from '../../../Types';
import type { InconsistencyField } from '../../../utils/inconsistencies';

interface GeneralSettingsInfoProps {
  generalSettings: DifficultyGeneralSettings[];
}

const CONSISTENCY_FIELDS: InconsistencyField<DifficultyGeneralSettings>[] = [
  { id: 'previewTime', getValue: (settings) => settings.previewTime },
];

function ModeCell({ mode }: { mode: string }) {
  return (
    <Group gap={6} wrap="nowrap" justify="center">
      <GameModeIcon mode={mode} size={16} color={getModeAccentColor(mode)} />
      <Text size="sm">{formatGameModeLabel(mode)}</Text>
    </Group>
  );
}

function StatusBadge({ value }: { value: boolean }) {
  return (
    <Badge color={value ? 'green' : 'gray'} variant={value ? 'light' : 'outline'}>
      {value ? 'Yes' : 'No'}
    </Badge>
  );
}

function CountdownCell({ settings }: { settings: DifficultyGeneralSettings }) {
  if (!settings.countdownSpeed) {
    return (
      <Badge color="gray" variant="outline">
        No
      </Badge>
    );
  }

  if (settings.countdownInsufficientTime) {
    return (
      <Tooltip
        multiline
        w={260}
        label="Countdown is enabled, but there is insufficient time before the first object for it to work"
      >
        <Badge color="yellow" variant="light" leftSection={<IconAlertTriangle size={12} />}>
          {settings.countdownSpeed}
        </Badge>
      </Tooltip>
    );
  }

  return (
    <Badge color="green" variant="light">
      {settings.countdownSpeed}
    </Badge>
  );
}

function GeneralSettingsInfo({ generalSettings }: GeneralSettingsInfoProps) {
  const theme = useMantineTheme();
  const groupCell = useGroupCellStyle(generalSettings, CONSISTENCY_FIELDS);

  if (generalSettings.length === 0) {
    return null;
  }

  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap="md">
        <Text fw={600}>General Settings</Text>

        <AppTable>
          <Table.Thead style={{ backgroundColor: theme.colors.dark[5] }}>
            <Table.Tr>
              <DifficultyTableHeaderCell>Difficulty</DifficultyTableHeaderCell>
              <Table.Th>Mode</Table.Th>
              <Table.Th>Audio File</Table.Th>
              <Table.Th>Lead-in</Table.Th>
              <Table.Th>Preview</Table.Th>
              <Table.Th>Stack Leniency</Table.Th>
              <Table.Th>Countdown</Table.Th>
              <Table.Th>Countdown Offset</Table.Th>
              <Table.Th>Letterbox in Breaks</Table.Th>
              <Table.Th>Widescreen Storyboard</Table.Th>
              <Table.Th>Use Skin Sprites</Table.Th>
              <Table.Th>Skin Preference</Table.Th>
              <Table.Th>Epilepsy Warning</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {generalSettings.map((settings) => (
              <Table.Tr key={itemKey(settings)}>
                <DifficultyTableCell>
                  <Text size="sm" fw={600}>
                    {settings.version}
                  </Text>
                </DifficultyTableCell>
                <Table.Td>
                  <ModeCell mode={settings.mode} />
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{settings.audioFileName}</Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{settings.audioLeadIn.toLocaleString()} ms</Text>
                </Table.Td>
                <Table.Td style={groupCell(settings, 'previewTime')}>
                  <Text size="sm">
                    <OsuLink text={settings.previewTimeFormatted} disableSeparators />
                  </Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{formatNullable(settings.stackLeniency)}</Text>
                </Table.Td>
                <Table.Td>
                  <CountdownCell settings={settings} />
                </Table.Td>
                <Table.Td>
                  <Text size="sm">
                    {settings.countdownSpeed !== null && settings.countdownOffset !== null
                      ? countWord(settings.countdownOffset, 'beat')
                      : 'N/A'}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <StatusBadge value={settings.letterboxInBreaks} />
                </Table.Td>
                <Table.Td>
                  <StatusBadge value={settings.widescreenStoryboard} />
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{formatNullable(settings.useSkinSprites)}</Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{formatNullable(settings.skinPreference, '(none)')}</Text>
                </Table.Td>
                <Table.Td>
                  <Text size="sm">{formatNullable(settings.epilepsyWarning)}</Text>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </AppTable>
      </Stack>
    </Paper>
  );
}

export default GeneralSettingsInfo;
