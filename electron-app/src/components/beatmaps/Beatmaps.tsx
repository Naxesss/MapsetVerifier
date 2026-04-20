import { Alert } from '@mantine/core';
import { IconAlertTriangle } from '@tabler/icons-react';
import BeatmapsList from './BeatmapsList.tsx';
import { useSettings } from '../../context/SettingsContext';

export default function Beatmaps() {
  const { settings } = useSettings();

  const songFolder = settings.songFolder;

  if (!songFolder) {
    return (
      <Alert icon={<IconAlertTriangle />} title="Song folder not set" color="yellow">
        Please set your song folder in the settings to view your beatmaps.
      </Alert>
    );
  }

  return (
    // Provide fallback empty string to satisfy required prop
    <BeatmapsList songFolder={settings.songFolder || ''} />
  );
}
