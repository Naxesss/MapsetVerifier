import {
  Alert,
  Text,
  Box,
  Flex,
  LoadingOverlay,
  SimpleGrid
} from '@mantine/core';
import { useMetadataAnalysis } from './hooks/useMetadataAnalysis';
import MetadataInfo from './MetadataInfo';
import ResourcesInfo from './ResourcesInfo';
import ColourSettings from './ColourSettings';
import { useBeatmap } from '../../../context/BeatmapContext';
import { useSettings } from '../../../context/SettingsContext';

function MetadataOverview() {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();

  const { data, isLoading, isError, error } = useMetadataAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  if (!folder) {
    return <Text>No BeatmapSet selected.</Text>;
  }

  if (!settings.songFolder) {
    return (
      <Alert color="yellow" title="Song folder not set" withCloseButton>
        <Text size="sm">Please set the song folder in settings to analyze metadata.</Text>
      </Alert>
    );
  }

  return (
    <Box>
      <LoadingOverlay visible={isLoading} zIndex={1000} overlayProps={{ radius: "sm", blur: 2 }} />
      {isError && (
        <Flex p="md">
          <Alert color="red" title="Error analyzing metadata" withCloseButton>
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
          <SimpleGrid cols={{ base: 1, lg: 2 }} spacing="md">
            <MetadataInfo difficulties={data.difficulties} />
            <ColourSettings colourSettings={data.colourSettings} />
          </SimpleGrid>
          <ResourcesInfo resources={data.resources} />
        </Flex>
      )}
    </Box>
  );
}

export default MetadataOverview;

