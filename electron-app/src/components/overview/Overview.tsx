import { Box } from '@mantine/core';
import { useEffect } from 'react';
import BeatmapOverview from './beatmap/BeatmapOverview.tsx';
import DifficultyOverview from './difficulty/DifficultyOverview.tsx';
import MetadataOverview from './metadata/MetadataOverview.tsx';
import ObjectsOverview from './objects/ObjectsOverview.tsx';
import OverviewPicker from './OverviewPicker.tsx';
import ResourcesOverview from './resources/ResourcesOverview.tsx';
import { useOverviewState } from '../../context/OverviewContext.tsx';
import { usePageHints } from '../../context/PageHintsContext.tsx';
import BeatmapHeader from '../common/BeatmapHeader.tsx';

function Overview() {
  const { setOverviewTab } = usePageHints();
  // Kept above the page, so coming back from Checks opens the page that was left open.
  const { tab: activeTab, setTab: setActiveTab } = useOverviewState();

  useEffect(() => {
    setOverviewTab(activeTab);
    return () => setOverviewTab(null);
  }, [activeTab, setOverviewTab]);

  return (
    <>
      <BeatmapHeader>
        <OverviewPicker section={activeTab} onSelect={setActiveTab} />
      </BeatmapHeader>
      <Box
        id="overview-panel"
        style={{ flex: 1, overflow: 'clip', position: 'relative' }}
        bg="dark.6"
      >
        {activeTab === 'General' && <MetadataOverview />}
        {activeTab === 'Beatmap' && <BeatmapOverview />}
        {activeTab === 'Difficulty' && <DifficultyOverview />}
        {activeTab === 'Resources' && <ResourcesOverview />}
        {activeTab === 'Objects' && <ObjectsOverview />}
      </Box>
    </>
  );
}

export default Overview;
