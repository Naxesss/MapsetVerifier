import { Alert, Button, Text } from '@mantine/core';
import { IconAlertTriangle, IconSettings } from '@tabler/icons-react';
import { useNavigate } from 'react-router-dom';
import BeatmapsList from './BeatmapsList.tsx';
import { useSettings } from '../../context/SettingsContext';

export default function Beatmaps() {
  const { settings } = useSettings();
  const navigate = useNavigate();

  const songFolder = settings.songFolder;
  const lazerLookupEnabled = settings.lazerLookupEnabled;

  if (!songFolder && !lazerLookupEnabled) {
    return (
      <Alert icon={<IconAlertTriangle />} title="Song folder not set" color="yellow">
        <Text size="sm" mb="sm">
          Please set your song folder in the settings to view your beatmaps.
        </Text>
        <Button
          variant="light"
          color="gray"
          leftSection={<IconSettings />}
          onClick={() => navigate('/settings')}
        >
          Open settings
        </Button>
      </Alert>
    );
  }

  return (
    <BeatmapsList
      songFolder={settings.songFolder}
      lazerLookupEnabled={lazerLookupEnabled}
      onOpenSettings={() => navigate('/settings')}
    />
  );
}
