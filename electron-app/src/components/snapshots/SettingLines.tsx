import { Badge, Box, Group, Stack, Text } from '@mantine/core';
import { DiffOpIcon } from './ChangeCounts';
import { describeSetting, settingLabel, settingValues } from './describe';
import { MicroLabel } from '../common/Headings.tsx';
import OsuLink from '../common/OsuLink.tsx';
import type { ApiSnapshotSettingChange } from '../../Types';

function SettingRow({ setting }: { setting: ApiSnapshotSettingChange }) {
  const { before, after } = settingValues(setting);
  const isSet = !!setting.added?.length || !!setting.removed?.length;

  return (
    <Box
      p="xs"
      px="sm"
      style={{
        borderRadius: 'var(--mantine-radius-sm)',
        background: 'var(--mantine-color-dark-7)',
      }}
    >
      <Group gap="xs" wrap="nowrap" align="flex-start">
        <Box mt={3} style={{ display: 'flex', flexShrink: 0 }}>
          <DiffOpIcon op={setting.op} />
        </Box>
        <Stack gap={4} style={{ flex: 1, minWidth: 0 }}>
          <Group gap="xs" wrap="nowrap" justify="space-between">
            <Text size="sm" fw={500} style={{ overflowWrap: 'anywhere' }}>
              <OsuLink
                text={
                  isSet || setting.op === 'Added' ? describeSetting(setting) : settingLabel(setting)
                }
              />
            </Text>
            {setting.appliesTo > 0 && (
              <Badge color="primary" style={{ flexShrink: 0 }}>
                All {setting.appliesTo} difficulties
              </Badge>
            )}
          </Group>
          {!isSet && setting.op === 'Changed' && (
            <Group gap="xs" wrap="nowrap" align="flex-start">
              <Text size="xs" c="dimmed" ff="monospace" style={{ overflowWrap: 'anywhere' }}>
                {before}
              </Text>
              <Text size="xs" c="yellow.5">
                {'→'}
              </Text>
              <Text size="xs" ff="monospace" style={{ overflowWrap: 'anywhere' }}>
                {after}
              </Text>
            </Group>
          )}
        </Stack>
      </Group>
    </Box>
  );
}

/** Settings, metadata, colours and events that changed, grouped under their section. */
export default function SettingLines({ settings }: { settings: ApiSnapshotSettingChange[] }) {
  const sections = [...new Set(settings.map((s) => s.section))];

  return (
    <Stack gap="md">
      {sections.map((section) => (
        <Stack key={section} gap="xs">
          <MicroLabel>{section}</MicroLabel>
          {settings
            .filter((s) => s.section === section)
            .map((s) => (
              <SettingRow
                key={`${s.section}|${s.key}|${s.op}|${s.after}|${s.before}`}
                setting={s}
              />
            ))}
        </Stack>
      ))}
    </Stack>
  );
}
