import { Box } from '@mantine/core';
import { useEffect } from 'react';
import AudioOverview from './audio/AudioOverview.tsx';
import BeatmapOverview from './beatmap/BeatmapOverview.tsx';
import DifficultyOverview from './difficulty/DifficultyOverview.tsx';
import MetadataOverview from './metadata/MetadataOverview.tsx';
import ObjectsOverview from './objects/ObjectsOverview.tsx';
import OverviewTabSelector from './OverviewTabSelector.tsx';
import VideoOverview from './video/VideoOverview.tsx';
import { useOverviewState } from '../../context/OverviewContext.tsx';
import { usePageHints } from '../../context/PageHintsContext.tsx';
import BeatmapHeader from '../common/BeatmapHeader.tsx';
import type { OverviewTab } from '../navbar/pageHints.tsx';

const TABS: OverviewTab[] = ['Metadata', 'Objects', 'Beatmap', 'Difficulty', 'Audio', 'Video'];

function Overview() {
  const { setOverviewTab } = usePageHints();
  // Kept above the page, so coming back from Checks opens the section that was left open.
  const { tab: activeTab, setTab: setActiveTab } = useOverviewState();

  useEffect(() => {
    setOverviewTab(activeTab);
    return () => setOverviewTab(null);
  }, [activeTab, setOverviewTab]);

  return (
    <>
      <BeatmapHeader>
        <OverviewTabSelector tabs={TABS} value={activeTab} onChange={setActiveTab} />
      </BeatmapHeader>
      <Box
        id="overview-panel"
        role="tabpanel"
        aria-labelledby={`overview-tab-${activeTab}`}
        style={{ flex: 1, overflow: 'clip', position: 'relative' }}
        bg="dark.6"
      >
        {activeTab === 'Metadata' && <MetadataOverview />}
        {activeTab === 'Beatmap' && <BeatmapOverview />}
        {activeTab === 'Difficulty' && <DifficultyOverview />}
        {activeTab === 'Audio' && <AudioOverview />}
        {activeTab === 'Video' && <VideoOverview />}
        {activeTab === 'Objects' && <ObjectsOverview />}
      </Box>
    </>
  );
}

export default Overview;
