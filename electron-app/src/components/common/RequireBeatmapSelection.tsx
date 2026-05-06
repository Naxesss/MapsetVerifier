import { Outlet } from 'react-router-dom';
import NoBeatmapsetDisplay from './NoBeatmapsetDisplay.tsx';
import { useBeatmap } from '../../context/BeatmapContext.tsx';

function RequireBeatmapSelection() {
  const { selectedFolder } = useBeatmap();

  if (!selectedFolder) {
    return <NoBeatmapsetDisplay />;
  }

  return <Outlet />;
}

export default RequireBeatmapSelection;
