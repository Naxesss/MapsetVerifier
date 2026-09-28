import { Box } from '@mantine/core';
import { useNavigate } from 'react-router-dom';
import BeatmapsList from './BeatmapsList.tsx';
import { SongsFolderMissing } from './MapsetListParts.tsx';
import { useSettings } from '../../context/SettingsContext';

export default function Beatmaps() {
  const { settings } = useSettings();
  const navigate = useNavigate();

  const songFolder = settings.songFolder;

  if (settings.beatmapViewMode === 'stable' && !songFolder) {
    return (
      <Box p="xs">
        <SongsFolderMissing onOpenSettings={() => navigate('/settings')} />
      </Box>
    );
  }

  return (
    <BeatmapsList
      songFolder={settings.songFolder}
      lazerDataDir={settings.lazerDataDir}
      onOpenSettings={() => navigate('/settings')}
    />
  );
}
