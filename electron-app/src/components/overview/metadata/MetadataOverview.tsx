import { SimpleGrid } from '@mantine/core';
import ColourSettings from './ColourSettings';
import { useMetadataAnalysis } from './hooks/useMetadataAnalysis';
import MetadataInfo from './MetadataInfo';
import { useBeatmap } from '../../../context/BeatmapContext';
import { useSettings } from '../../../context/SettingsContext';
import AnalysisTab from '../AnalysisTab.tsx';

function MetadataOverview() {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();

  const { data, isLoading, isError, error } = useMetadataAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  return (
    <AnalysisTab
      data={data}
      isLoading={isLoading}
      isError={isError}
      error={error}
      subject="metadata"
    >
      {(data) => (
        <SimpleGrid cols={{ base: 1, lg: 2 }} spacing="md">
          <MetadataInfo difficulties={data.difficulties} />
          <ColourSettings colourSettings={data.colourSettings} />
        </SimpleGrid>
      )}
    </AnalysisTab>
  );
}

export default MetadataOverview;
