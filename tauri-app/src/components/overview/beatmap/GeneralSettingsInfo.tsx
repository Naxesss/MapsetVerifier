import { Text, Badge, Group, Paper, useMantineTheme, Stack, Box, SimpleGrid } from '@mantine/core';
import { IconCheck, IconX } from '@tabler/icons-react';
import { DifficultyGeneralSettings } from '../../../Types';
import { groupByValue, ValueGroup } from './utils/groupByValue';

interface GeneralSettingsInfoProps {
  generalSettings: DifficultyGeneralSettings[];
}

function getGeneralSettingsKey(settings: DifficultyGeneralSettings): string {
  const parts = [
    settings.audioFileName,
    settings.audioLeadIn,
    settings.stackLeniency ?? 'na',
    settings.hasCountdown,
    settings.countdownSpeed ?? 'na',
    settings.countdownOffset ?? 'na',
    settings.letterboxInBreaks,
    settings.widescreenStoryboard,
    settings.previewTime,
    settings.useSkinSprites ?? 'na',
    settings.skinPreference,
    settings.epilepsyWarning ?? 'na',
  ];
  return parts.join('|');
}

function SettingRow({ label, value }: { label: string; value: string | number | null | undefined }) {
  if (value === null || value === undefined) return null;
  return (
    <Group justify="space-between" gap="xs">
      <Text size="xs" c="dimmed">{label}</Text>
      <Text size="xs" fw={500}>{value}</Text>
    </Group>
  );
}

function BooleanRow({ label, value }: { label: string; value: boolean }) {
  const theme = useMantineTheme();
  return (
    <Group justify="space-between" gap="xs">
      <Text size="xs" c="dimmed">{label}</Text>
      {value ? (
        <IconCheck size={14} color={theme.colors.green[5]} />
      ) : (
        <IconX size={14} color={theme.colors.gray[6]} />
      )}
    </Group>
  );
}

function GeneralSettingsGroup({ group }: { group: ValueGroup<DifficultyGeneralSettings> }) {
  const theme = useMantineTheme();
  const settings = group.value;

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

      <SimpleGrid cols={2} spacing="xs">
        <Stack gap={4}>
          <SettingRow label="Audio Filename" value={settings.audioFileName} />
          <SettingRow label="Audio Lead-in" value={`${settings.audioLeadIn} ms`} />
          <SettingRow label="Stack Leniency" value={settings.stackLeniency ?? 'N/A'} />
          <SettingRow label="Preview Time" value={settings.previewTimeFormatted} />
          <SettingRow label="Skin Preference" value={settings.skinPreference || '(none)'} />
        </Stack>
        <Stack gap={4}>
          <SettingRow 
            label="Countdown" 
            value={settings.hasCountdown ? settings.countdownSpeed : 'None'} 
          />
          {settings.hasCountdown && settings.countdownOffset !== null && (
            <SettingRow label="Countdown Offset" value={settings.countdownOffset} />
          )}
          <BooleanRow label="Letterbox in Breaks" value={settings.letterboxInBreaks} />
          <BooleanRow label="Widescreen Storyboard" value={settings.widescreenStoryboard} />
          <SettingRow label="Use Skin Sprites" value={settings.useSkinSprites ?? 'N/A'} />
          <SettingRow label="Epilepsy Warning" value={settings.epilepsyWarning ?? 'N/A'} />
        </Stack>
      </SimpleGrid>
    </Box>
  );
}

function GeneralSettingsInfo({ generalSettings }: GeneralSettingsInfoProps) {
  const theme = useMantineTheme();

  if (generalSettings.length === 0) {
    return null;
  }

  const groups = groupByValue(generalSettings, getGeneralSettingsKey);

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
      <Text fw={600} mb="md">General Settings</Text>
      <Stack gap="sm">
        {groups.map((group) => (
          <GeneralSettingsGroup key={group.key} group={group} />
        ))}
      </Stack>
    </Paper>
  );
}

export default GeneralSettingsInfo;

