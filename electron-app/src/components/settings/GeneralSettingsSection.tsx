import { Button, Group, Select, SegmentedControl, TextInput } from '@mantine/core';
import { IconFolder, IconSettings } from '@tabler/icons-react';
import { SettingsRow, SettingsSection } from './SettingsSection';
import { BeatmapViewMode, useSettings } from '../../context/SettingsContext';
import {
  DEFAULT_UI_FONT_FAMILY,
  UI_FONT_FAMILY_OPTIONS,
  parseUiFontFamily,
} from '../../theme/fonts';

export default function GeneralSettingsSection() {
  const { settings, setSettings } = useSettings();
  const viewMode = settings.beatmapViewMode;
  const showLazerDataDir = viewMode === 'lazer' || viewMode === 'both';
  const showSongFolder = viewMode === 'stable' || viewMode === 'both';

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

  const pickLazerDataDir = async () => {
    try {
      const result = await window.electronAPI?.dialog.openFolder();
      if (typeof result === 'string') {
        setSettings((prev) => ({ ...prev, lazerDataDir: result }));
      }
    } catch (e: any) {
      console.error('[Settings] Lazer data folder pick failed:', e);
      const msg = typeof e === 'string' ? e : e?.message || 'Unknown error';
      alert('Folder picker failed: ' + msg);
    }
  };

  return (
    <SettingsSection
      icon={<IconSettings size={28} />}
      title="General"
      description="Core app preferences and the beatmap library used by the sidebar."
    >
      <SettingsRow
        title="Beatmap library"
        description="Which beatmap library the sidebar reads from. On macOS/Linux, set the lazer data folder manually below; the 'currently open in editor' shortcut is Windows-only."
        control={
          <SegmentedControl
            data={[
              { label: 'Stable', value: 'stable' },
              { label: 'Lazer', value: 'lazer' },
              { label: 'Both', value: 'both' },
            ]}
            value={viewMode}
            onChange={(value) =>
              setSettings((prev) => ({ ...prev, beatmapViewMode: value as BeatmapViewMode }))
            }
          />
        }
      />
      {showSongFolder && (
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
      )}
      {showLazerDataDir && (
        <Group align="flex-end" gap="sm" wrap="nowrap">
          <TextInput
            label="osu!(lazer) data folder"
            description="Contains client.realm. Auto-detected when left empty."
            value={settings.lazerDataDir ?? ''}
            readOnly
            style={{ flex: 1, minWidth: 0 }}
            onClick={() => !settings.lazerDataDir && pickLazerDataDir()}
          />
          <Button
            size="sm"
            variant="light"
            leftSection={<IconFolder size={18} />}
            onClick={pickLazerDataDir}
          >
            Browse
          </Button>
        </Group>
      )}
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
