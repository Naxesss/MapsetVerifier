import { Alert, Text, Box, Flex, LoadingOverlay, Stack, SimpleGrid } from '@mantine/core';
import { IconAlertCircle, IconAlertTriangle, IconRulerMeasure } from '@tabler/icons-react';
import { useEffect } from 'react';
import BitrateGraph from './BitrateGraph';
import ChannelBalance from './ChannelBalance';
import DynamicRange from './DynamicRange';
import FormatInfo from './FormatInfo';
import FrequencyAnalysis from './FrequencyAnalysis';
import { useAudioAnalysis, useFrequencyAnalysis } from './hooks/useAudioAnalysis';
import Spectrogram from './Spectrogram';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import NoBeatmapsetDisplay from '../../common/NoBeatmapsetDisplay.tsx';
import StackTraceMessage from '../../common/StackTraceMessage.tsx';

interface AudioOverviewProps {
  reloadFlag: number;
}

function AudioOverview({ reloadFlag }: AudioOverviewProps) {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();

  const {
    data,
    isLoading,
    isError,
    error,
    refetch: audioRefetch,
  } = useAudioAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  const {
    data: frequencyData,
    isLoading: frequencyLoading,
    refetch: frequencyRefetch,
  } = useFrequencyAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  useEffect(() => {
    audioRefetch();
    frequencyRefetch();
  }, [reloadFlag]);

  if (!folder) {
    return <NoBeatmapsetDisplay />;
  }

  const durationMs = data?.formatAnalysis?.durationMs || 0;

  return (
    <Box>
      <LoadingOverlay
        visible={isLoading || frequencyLoading}
        zIndex={1000}
        overlayProps={{ radius: 'sm', blur: 2 }}
      />
      {isError && (
        <Flex p="md">
          <Alert icon={<IconAlertCircle />} color="red" title="Error analyzing audio">
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
          {data.complianceIssues?.length > 0 && (
            <Alert icon={<IconRulerMeasure />} color="yellow" title="Compliance Issues">
              <Stack gap="xs">
                {data.complianceIssues.map((issue: string, idx: number) => (
                  <Text key={idx} size="sm">
                    • {issue}
                  </Text>
                ))}
              </Stack>
            </Alert>
          )}
          <SimpleGrid cols={{ base: 1, lg: 2 }} spacing="md">
            {data.formatAnalysis && (
              <FormatInfo data={data.formatAnalysis} audioFilePath={data.audioFilePath} />
            )}
            {data.bitrateAnalysis && <BitrateGraph data={data.bitrateAnalysis} />}
            <Spectrogram folder={folder} songFolder={settings.songFolder ?? ''} />
          </SimpleGrid>

          {settings.showAdvancedAudioAnalysis && (
            <>
              <Text fw={600}>Advanced Audio Analysis</Text>
              <SimpleGrid cols={{ base: 1, md: 2, lg: 3 }} spacing="md">
                {data.channelAnalysis && (
                  <ChannelBalance data={data.channelAnalysis} durationMs={durationMs} />
                )}
                {data.dynamicRangeAnalysis && (
                  <DynamicRange data={data.dynamicRangeAnalysis} durationMs={durationMs} />
                )}
                <FrequencyAnalysis data={frequencyData} isLoading={frequencyLoading} />
              </SimpleGrid>
            </>
          )}
        </Flex>
      )}
    </Box>
  );
}

export default AudioOverview;
