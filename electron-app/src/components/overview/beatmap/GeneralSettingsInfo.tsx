import { Badge, Group, Stack, Table, Text, Tooltip, useMantineTheme } from '@mantine/core';
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
import SectionCard from '../../common/SectionCard.tsx';
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
    <Group gap="xs" wrap="nowrap" justify="center">
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
    return <Badge color="gray">No</Badge>;
  }

  if (settings.countdownInsufficientTime) {
    return (
      <Tooltip
        multiline
        w={260}
        label="Countdown is enabled, but there is insufficient time before the first object for it to work"
      >
        <Badge color="yellow" leftSection={<IconAlertTriangle size={12} />}>
          {settings.countdownSpeed}
        </Badge>
      </Tooltip>
    );
  }

  return <Badge color="green">{settings.countdownSpeed}</Badge>;
}

function GeneralSettingsInfo({ generalSettings }: GeneralSettingsInfoProps) {
  const theme = useMantineTheme();
  const groupCell = useGroupCellStyle(generalSettings, CONSISTENCY_FIELDS);
  // One mode for the whole mapset is said once by the mode icons elsewhere, not on every row.
  const showMode = new Set(generalSettings.map((entry) => entry.mode)).size > 1;

  if (generalSettings.length === 0) {
    return null;
  }

  return (
    <SectionCard title="General settings">
      <Stack gap="md">
        <AppTable>
          <Table.Thead style={{ backgroundColor: theme.colors.dark[5] }}>
            <Table.Tr>
              <DifficultyTableHeaderCell>Difficulty</DifficultyTableHeaderCell>
              {showMode && <Table.Th>Mode</Table.Th>}
              <Table.Th>Audio file</Table.Th>
              <Table.Th>Lead-in</Table.Th>
              <Table.Th>Preview</Table.Th>
              <Table.Th>Stack leniency</Table.Th>
              <Table.Th>Countdown</Table.Th>
              <Table.Th>Countdown offset</Table.Th>
              <Table.Th>Letterbox in Breaks</Table.Th>
              <Table.Th>Widescreen storyboard</Table.Th>
              <Table.Th>Use skin sprites</Table.Th>
              <Table.Th>Skin preference</Table.Th>
              <Table.Th>Epilepsy warning</Table.Th>
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
                {showMode && (
                  <Table.Td>
                    <ModeCell mode={settings.mode} />
                  </Table.Td>
                )}
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
    </SectionCard>
  );
}

export default GeneralSettingsInfo;
