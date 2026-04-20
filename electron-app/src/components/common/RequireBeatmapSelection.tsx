import {Alert} from '@mantine/core';
import {IconAlertTriangle} from "@tabler/icons-react";
import { Outlet } from 'react-router-dom';
import { useBeatmap } from '../../context/BeatmapContext.tsx';

function RequireBeatmapSelection() {
  const { selectedFolder } = useBeatmap();

  if (!selectedFolder) {
    return (
      <Alert title="No beatmapset selected" color="yellow" icon={<IconAlertTriangle />}>
        To use this page you first need to select a beatmapset on the left side menu.
      </Alert>
    )
  }

  return <Outlet />;
}

export default RequireBeatmapSelection;
