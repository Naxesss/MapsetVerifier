import { Alert, Button, Text } from '@mantine/core';
import { IconAlertTriangle, IconSettings } from '@tabler/icons-react';
import { useState } from 'react';
import BeatmapsList from './BeatmapsList.tsx';
import { useSettings } from '../../context/SettingsContext';
import SettingsModal from '../settings/SettingsModal';

export default function Beatmaps() {
  const { settings } = useSettings();
  const [settingsOpened, setSettingsOpened] = useState(false);

  const songFolder = settings.songFolder;

  if (!songFolder) {
    return (
      <>
        <Alert icon={<IconAlertTriangle />} title="Song folder not set" color="yellow">
          <Text size="sm" mb="sm">
            Please set your song folder in the settings to view your beatmaps.
          </Text>
          <Button variant="light" color="gray" leftSection={<IconSettings />} onClick={() => setSettingsOpened(true)}>
            Open settings
          </Button>
        </Alert>
        <SettingsModal opened={settingsOpened} onClose={() => setSettingsOpened(false)} />
      </>
    );
  }

  return (
    // Provide fallback empty string to satisfy required prop
    <BeatmapsList songFolder={settings.songFolder || ''} />
  );
}
