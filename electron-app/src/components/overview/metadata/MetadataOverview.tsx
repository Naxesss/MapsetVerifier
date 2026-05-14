import { Alert, Text, Box, Flex, LoadingOverlay, SimpleGrid } from '@mantine/core';
import { IconAlertCircle, IconAlertTriangle } from '@tabler/icons-react';
import { useEffect } from 'react';
import ColourSettings from './ColourSettings';
import { useMetadataAnalysis } from './hooks/useMetadataAnalysis';
import MetadataInfo from './MetadataInfo';
import ResourcesInfo from './ResourcesInfo';
import { useBeatmap } from '../../../context/BeatmapContext';
import { useSettings } from '../../../context/SettingsContext';
import NoBeatmapsetDisplay from '../../common/NoBeatmapsetDisplay.tsx';
import StackTraceMessage from '../../common/StackTraceMessage.tsx';

interface MetadataOverviewProps {
  reloadFlag: number;
}

function MetadataOverview({ reloadFlag }: MetadataOverviewProps) {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();

  const { data, isLoading, isError, error, refetch } = useMetadataAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  useEffect(() => {
    refetch();
  }, [reloadFlag]);

  if (!folder) {
    return <NoBeatmapsetDisplay />;
  }

  return (
    <Box>
      <LoadingOverlay visible={isLoading} zIndex={1000} overlayProps={{ radius: 'sm', blur: 2 }} />
      {isError && (
        <Flex p="md">
          <Alert icon={<IconAlertCircle />} color="red" title="Error analyzing metadata">
            <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
              {error?.message}
            </Text>
            {error?.stackTrace && <StackTraceMessage stackTrace={error.stackTrace} />}
          </Alert>
        </Flex>
      )}

      {data && !data.success && (
        <Flex p="md">
          <Alert icon={<IconAlertTriangle />} color="yellow" title="Analysis failed">
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
