import { Box, useMantineTheme } from '@mantine/core';
import { useEffect, useState } from 'react';
import AudioOverview from './audio/AudioOverview.tsx';
import BeatmapOverview from './beatmap/BeatmapOverview.tsx';
import DifficultyOverview from './difficulty/DifficultyOverview.tsx';
import MetadataOverview from './metadata/MetadataOverview.tsx';
import ObjectsOverview from './objects/ObjectsOverview.tsx';
import OverviewTabSelector from './OverviewTabSelector.tsx';
import VideoOverview from './video/VideoOverview.tsx';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { useBeatmapReparse } from '../../context/BeatmapReparseRegistry.tsx';
import { usePageHints } from '../../context/PageHintsContext.tsx';
import BeatmapActionButtons from '../checks/BeatmapActionButtons';
import BeatmapHeader from '../common/BeatmapHeader.tsx';
import type { OverviewTab } from '../navbar/pageHints.tsx';

const TABS: OverviewTab[] = ['Metadata', 'Objects', 'Beatmap', 'Difficulty', 'Audio', 'Video'];

function Overview() {
  const theme = useMantineTheme();
  const { beatmapFolderPath, beatmapInfo } = useBeatmap();
  const { triggerReparse } = useBeatmapReparse();
  const { setOverviewTab } = usePageHints();
  const [activeTab, setActiveTab] = useState<OverviewTab>('Metadata');

  useEffect(() => {
    setOverviewTab(activeTab);
    return () => setOverviewTab(null);
  }, [activeTab, setOverviewTab]);

  return (
    <Box
      h="100%"
      style={{
        fontFamily: theme.headings.fontFamily,
        position: 'relative',
        width: '100%',
        borderRadius: theme.radius.lg,
        overflow: 'hidden',
        // Clip the banner's layers in one pass, so its rounded top corners stay clean.
        isolation: 'isolate',
        boxShadow: '0 4px 32px rgba(0,0,0,0.4)',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'flex-start',
      }}
    >
      <BeatmapHeader>
        <BeatmapActionButtons
          beatmapFolderPath={beatmapFolderPath}
          beatmapId={beatmapInfo?.beatmapId ?? undefined}
          beatmapSetId={beatmapInfo?.beatmapSetId ?? undefined}
          onReparse={triggerReparse}
        />
        <OverviewTabSelector tabs={TABS} value={activeTab} onChange={setActiveTab} />
      </BeatmapHeader>
      <Box style={{ flex: 1, overflow: 'auto', position: 'relative' }} bg="dark.6">
        {activeTab === 'Metadata' && <MetadataOverview />}
        {activeTab === 'Beatmap' && <BeatmapOverview />}
        {activeTab === 'Difficulty' && <DifficultyOverview />}
        {activeTab === 'Audio' && <AudioOverview />}
        {activeTab === 'Video' && <VideoOverview />}
        {activeTab === 'Objects' && <ObjectsOverview />}
      </Box>
    </Box>
  );
}

export default Overview;
