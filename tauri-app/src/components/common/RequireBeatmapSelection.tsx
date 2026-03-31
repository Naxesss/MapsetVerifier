import { Text } from '@mantine/core';
import { Outlet } from 'react-router-dom';
import { useBeatmap } from '../../context/BeatmapContext.tsx';

function RequireBeatmapSelection() {
  const { selectedFolder } = useBeatmap();

  if (!selectedFolder) {
    return <Text>No BeatmapSet selected.</Text>;
  }

  return <Outlet />;
}

export default RequireBeatmapSelection;