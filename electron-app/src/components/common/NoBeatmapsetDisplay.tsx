import { IconGhost2 } from '@tabler/icons-react';
import EmptyState from './EmptyState.tsx';

function NoBeatmapsetDisplay() {
  return (
    <EmptyState
      fullHeight
      icon={IconGhost2}
      title="No mapset selected"
      description="Pick a mapset from the list on the left. The mapset open in osu! is shown at the top of the list."
    />
  );
}

export default NoBeatmapsetDisplay;
