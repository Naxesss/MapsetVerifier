import {
  Alert,
  Text,
  Box,
  Flex,
  LoadingOverlay,
  SimpleGrid
} from '@mantine/core';
import { useBeatmapAnalysis } from './hooks/useBeatmapAnalysis';
import StatisticsInfo from './StatisticsInfo';
import GeneralSettingsInfo from './GeneralSettingsInfo';
import DifficultySettingsInfo from './DifficultySettingsInfo';
import { useBeatmap } from '../../../context/BeatmapContext';
import { useSettings } from '../../../context/SettingsContext';

function BeatmapOverview() {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();

  const { data, isLoading, isError, error } = useBeatmapAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  if (!folder) {
    return <Text>No BeatmapSet selected.</Text>;
  }

  if (!settings.songFolder) {
    return (
      <Alert color="yellow" title="Song folder not set">
        <Text size="sm">Please set the song folder in settings to analyze beatmaps.</Text>
      </Alert>
    );
  }

  return (
    <Box>
      <LoadingOverlay visible={isLoading} zIndex={1000} overlayProps={{ radius: "sm", blur: 2 }} />
      {isError && (
        <Flex p="md">
          <Alert color="red" title="Error analyzing beatmap">
            <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>{error?.message}</Text>
            {error?.stackTrace && (
              <Text mt="sm" size="xs" c="red.3" style={{ whiteSpace: 'pre-wrap' }}>{error.stackTrace}</Text>
            )}
          </Alert>
        </Flex>
      )}

      {data && !data.success && (
        <Flex p="md">
          <Alert color="yellow" title="Analysis failed">
            <Text size="sm">{data.errorMessage}</Text>
          </Alert>
        </Flex>
      )}

      {data && data.success && (
        <Flex gap="md" p="md" direction="column">
          <StatisticsInfo statistics={data.statistics} />
          <SimpleGrid cols={{ base: 1, lg: 2 }} spacing="md">
            <GeneralSettingsInfo generalSettings={data.generalSettings} />
            <DifficultySettingsInfo difficultySettings={data.difficultySettings} />
          </SimpleGrid>
        </Flex>
      )}
    </Box>
  );
}

export default BeatmapOverview;

