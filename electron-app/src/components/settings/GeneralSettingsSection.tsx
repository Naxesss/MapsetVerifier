import { Button, Group, Select, TextInput } from '@mantine/core';
import { IconFolder, IconSettings } from '@tabler/icons-react';
import { SettingsRow, SettingsSection } from './SettingsSection';
import { useSettings } from '../../context/SettingsContext';
import {
  DEFAULT_UI_FONT_FAMILY,
  UI_FONT_FAMILY_OPTIONS,
  parseUiFontFamily,
} from '../../theme/fonts';

export default function GeneralSettingsSection() {
  const { settings, setSettings } = useSettings();

  const pickFolder = async () => {
    try {
      const result = await window.electronAPI?.dialog.openFolder();
      if (typeof result === 'string') {
        setSettings((prev) => ({ ...prev, songFolder: result }));
      }
    } catch (e: any) {
      console.error('[Settings] Folder pick failed:', e);
      const msg = typeof e === 'string' ? e : e?.message || 'Unknown error';
      alert('Folder picker failed: ' + msg);
    }
  };

  return (
    <SettingsSection
      icon={<IconSettings size={28} />}
      title="General"
      description="Core app preferences and the osu!stable Songs folder used by the sidebar."
    >
      <Group align="flex-end" gap="sm" wrap="nowrap">
        <TextInput
          label="osu! Songs Folder"
          value={settings.songFolder ?? ''}
          readOnly
          style={{ flex: 1, minWidth: 0 }}
          onClick={() => !settings.songFolder && pickFolder()}
        />
        <Button
          size="sm"
          variant="light"
          leftSection={<IconFolder size={18} />}
          onClick={pickFolder}
        >
          Browse
        </Button>
      </Group>
      <SettingsRow
        title="Font"
        description="Controls the interface font throughout the app."
        control={
          <Select
            data={UI_FONT_FAMILY_OPTIONS}
            value={settings.uiFontFamily}
            allowDeselect={false}
            w={220}
            onChange={(value) => {
              const font = parseUiFontFamily(value ?? DEFAULT_UI_FONT_FAMILY);
              setSettings((prev) => ({ ...prev, uiFontFamily: font }));
            }}
          />
        }
      />
    </SettingsSection>
  );
}
