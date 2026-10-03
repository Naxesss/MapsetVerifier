import { Box } from '@mantine/core';
import { useEffect } from 'react';
import AudioOverview from './audio/AudioOverview.tsx';
import BeatmapOverview from './beatmap/BeatmapOverview.tsx';
import DifficultyOverview from './difficulty/DifficultyOverview.tsx';
import MetadataOverview from './metadata/MetadataOverview.tsx';
import ObjectsOverview from './objects/ObjectsOverview.tsx';
import OverviewPicker from './OverviewPicker.tsx';
import OverviewSummary from './OverviewSummary.tsx';
import { useUnavailableSections } from './useUnavailableSections.ts';
import VideoOverview from './video/VideoOverview.tsx';
import { useOverviewState } from '../../context/OverviewContext.tsx';
import { usePageHints } from '../../context/PageHintsContext.tsx';
import BeatmapHeader from '../common/BeatmapHeader.tsx';

function Overview() {
  const { setOverviewTab } = usePageHints();
  // Kept above the page, so coming back from Checks opens the page that was left open.
  const { tab: activeTab, setTab: setActiveTab } = useOverviewState();

  // A mapset without video has no Video page to be left open on.
  const unavailable = useUnavailableSections();
  const isUnavailable = !!unavailable[activeTab];
  useEffect(() => {
    if (isUnavailable) setActiveTab('Summary');
  }, [isUnavailable, setActiveTab]);

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
        {activeTab === 'Summary' && <OverviewSummary onOpen={setActiveTab} />}
        {activeTab === 'General' && <MetadataOverview />}
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
