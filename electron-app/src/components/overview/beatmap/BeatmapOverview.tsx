import { Alert, Text, Box, Flex, LoadingOverlay } from '@mantine/core';
import { IconAlertCircle, IconAlertTriangle } from '@tabler/icons-react';
import { useEffect } from 'react';
import DifficultySettingsInfo from './DifficultySettingsInfo';
import GeneralSettingsInfo from './GeneralSettingsInfo';
import { useBeatmapAnalysis } from './hooks/useBeatmapAnalysis';
import StatisticsInfo from './StatisticsInfo';
import { useBeatmap } from '../../../context/BeatmapContext';
import { useSettings } from '../../../context/SettingsContext';
import StackTraceMessage from '../../common/StackTraceMessage.tsx';

interface BeatmapOverviewProps {
  reloadFlag: number;
}

function BeatmapOverview({ reloadFlag }: BeatmapOverviewProps) {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();

  const { data, isLoading, isError, error, refetch } = useBeatmapAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  useEffect(() => {
    refetch();
  }, [reloadFlag]);

  if (!folder) {
    return (
      <Alert icon={<IconAlertTriangle />} color="yellow" title="No beatmapset selected">
        <Text size="sm">Select a beatmapset from the sidebar to analyze beatmaps.</Text>
      </Alert>
    );
  }

  return (
    <Box>
      <LoadingOverlay visible={isLoading} zIndex={1000} overlayProps={{ radius: 'sm', blur: 2 }} />
      {isError && (
        <Flex p="md">
          <Alert icon={<IconAlertCircle />} color="red" title="Error analyzing beatmap">
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
          <StatisticsInfo statistics={data.statistics} />
          <GeneralSettingsInfo generalSettings={data.generalSettings} />
          <DifficultySettingsInfo difficultySettings={data.difficultySettings} />
        </Flex>
      )}
    </Box>
  );
}

export default BeatmapOverview;
