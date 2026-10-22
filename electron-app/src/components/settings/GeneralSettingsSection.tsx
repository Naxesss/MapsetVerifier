import { Select, SegmentedControl } from '@mantine/core';
import { IconSettings } from '@tabler/icons-react';
import FolderField from './FolderField';
import { SettingsRow, SettingsSection } from './SettingsSection';
import { BeatmapViewMode, useSettings } from '../../context/SettingsContext';
import {
  DEFAULT_UI_FONT_FAMILY,
  UI_FONT_FAMILY_OPTIONS,
  parseUiFontFamily,
} from '../../theme/fonts';
import { UI_ZOOM_OPTIONS, parseUiZoomPercent } from '../../theme/zoom';
import { isWindowsPlatform } from '../../utils/platform.ts';
import type { ClockFormat } from '../../utils/dateTime';

const LIBRARY_DESCRIPTION = 'Which beatmap library the sidebar reads from.';
const LIBRARY_DESCRIPTION_NON_WINDOWS = `${LIBRARY_DESCRIPTION} Set the lazer data folder manually below; showing the mapset open in the editor only works on Windows.`;

export default function GeneralSettingsSection() {
  const { settings, setSettings } = useSettings();
  const viewMode = settings.beatmapViewMode;
  const showLazerDataDir = viewMode === 'lazer' || viewMode === 'both';
  const showSongFolder = viewMode === 'stable' || viewMode === 'both';

  return (
    <SettingsSection
      icon={<IconSettings size={28} />}
      title="General"
      description="Core app preferences and the beatmap library used by the sidebar."
    >
      <SettingsRow
        title="Beatmap library"
        description={isWindowsPlatform() ? LIBRARY_DESCRIPTION : LIBRARY_DESCRIPTION_NON_WINDOWS}
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
        <FolderField
          label="osu! Songs folder"
          value={settings.songFolder}
          onChange={(songFolder) => setSettings((prev) => ({ ...prev, songFolder }))}
        />
      )}
      {showLazerDataDir && (
        <FolderField
          label="osu!(lazer) data folder"
          description="Contains client.realm. Auto-detected when left empty."
          value={settings.lazerDataDir}
          onChange={(lazerDataDir) => setSettings((prev) => ({ ...prev, lazerDataDir }))}
        />
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
      <SettingsRow
        title="Zoom"
        description="Default interface scale, useful on high-resolution screens. Ctrl+= and Ctrl+- adjust it temporarily; Ctrl+0 returns to this value."
        control={
          <Select
            data={UI_ZOOM_OPTIONS}
            value={String(settings.uiZoomPercent)}
            allowDeselect={false}
            w={220}
            onChange={(value) => {
              const uiZoomPercent = parseUiZoomPercent(value);
              setSettings((prev) => ({ ...prev, uiZoomPercent }));
            }}
          />
        }
      />
      <SettingsRow
        title="Time format"
        description="How times are shown throughout the app, such as in snapshots and check runs."
        control={
          <SegmentedControl
            data={[
              { label: '24-hour', value: '24h' },
              { label: '12-hour', value: '12h' },
            ]}
            value={settings.clockFormat}
            onChange={(value) =>
              setSettings((prev) => ({ ...prev, clockFormat: value as ClockFormat }))
            }
          />
        }
      />
    </SettingsSection>
  );
}
