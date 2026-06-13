import { Box, Group, LoadingOverlay, SegmentedControl, useMantineTheme } from '@mantine/core';
import { useEffect, useState } from 'react';
import AudioOverview from './audio/AudioOverview.tsx';
import BeatmapOverview from './beatmap/BeatmapOverview.tsx';
import DifficultyOverview from './difficulty/DifficultyOverview.tsx';
import MetadataOverview from './metadata/MetadataOverview.tsx';
import ObjectsOverview from './objects/ObjectsOverview.tsx';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { useBeatmapReparse } from '../../context/BeatmapReparseRegistry.tsx';
import { usePageHints } from '../../context/PageHintsContext.tsx';
import { useSettings } from '../../context/SettingsContext.tsx';
import BeatmapActionButtons from '../checks/BeatmapActionButtons';
import { useBeatmapBackground } from '../checks/hooks/useBeatmapBackground.ts';
import BeatmapHeader from '../common/BeatmapHeader.tsx';
import type { OverviewTab } from '../navbar/pageHints.tsx';

const TABS: OverviewTab[] = ['Metadata', 'Objects', 'Beatmap', 'Difficulty', 'Audio'];

function Overview() {
  const theme = useMantineTheme();
  const { selectedFolder, beatmapFolderPath, beatmapInfo } = useBeatmap();
  const { triggerReparse } = useBeatmapReparse();
  const { settings } = useSettings();
  const { setOverviewTab } = usePageHints();
  const { bgUrl, isLoading } = useBeatmapBackground(selectedFolder, settings.songFolder);
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
        boxShadow: '0 4px 32px rgba(0,0,0,0.4)',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'flex-start',
      }}
    >
      <LoadingOverlay visible={isLoading} zIndex={1000} overlayProps={{ radius: 'sm', blur: 2 }} />
      <BeatmapHeader bgUrl={bgUrl}>
        <Group gap="sm" justify="space-between" style={{ width: '100%' }}>
          <BeatmapActionButtons
            beatmapFolderPath={beatmapFolderPath}
            beatmapId={beatmapInfo?.beatmapId ?? undefined}
            beatmapSetId={beatmapInfo?.beatmapSetId ?? undefined}
            onReparse={triggerReparse}
          />
          <SegmentedControl
            value={activeTab}
            onChange={(value) => setActiveTab(value as OverviewTab)}
            data={TABS}
            size="xs"
          />
        </Group>
      </BeatmapHeader>
      <Box style={{ flex: 1, overflow: 'auto' }} bg="dark.6">
        {activeTab === 'Metadata' && <MetadataOverview />}
        {activeTab === 'Beatmap' && <BeatmapOverview />}
        {activeTab === 'Difficulty' && <DifficultyOverview />}
        {activeTab === 'Audio' && <AudioOverview />}
        {activeTab === 'Objects' && <ObjectsOverview />}
      </Box>
    </Box>
  );
}

export default Overview;
