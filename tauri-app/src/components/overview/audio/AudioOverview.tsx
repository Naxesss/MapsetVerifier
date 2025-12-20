import {
  Alert,
  Text,
  Box,
  Flex,
  LoadingOverlay,
  Stack,
  SimpleGrid
} from '@mantine/core';
import { useAudioAnalysis, useFrequencyAnalysis } from './hooks/useAudioAnalysis';
import BitrateGraph from './BitrateGraph';
import ChannelBalance from './ChannelBalance';
import DynamicRange from './DynamicRange';
import FormatInfo from './FormatInfo';
import Spectrogram from './Spectrogram';
import FrequencyAnalysis from './FrequencyAnalysis';
import {useBeatmap} from "../../../context/BeatmapContext.tsx";
import {useSettings} from "../../../context/SettingsContext.tsx";

function AudioOverview() {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();

  const { data, isLoading, isError, error } = useAudioAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  const { data: frequencyData, isLoading: frequencyLoading } = useFrequencyAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  if (!folder) {
    return <Text>No BeatmapSet selected.</Text>;
  }

  if (!settings.songFolder) {
    return (
      <Alert color="yellow" title="Song folder not set" withCloseButton>
        <Text size="sm">Please set the song folder in settings to analyze audio.</Text>
      </Alert>
    );
  }
  
  const durationMs = data?.formatAnalysis?.durationMs || 0;

  return (
    <Box>
      <LoadingOverlay visible={isLoading || frequencyLoading} zIndex={1000} overlayProps={{ radius: "sm", blur: 2 }} />
      {isError && (
        <Flex p="md">
          <Alert color="red" title="Error analyzing audio" withCloseButton>
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
          {data.complianceIssues?.length > 0 && (
            <Alert color="yellow" title="Compliance Issues">
              <Stack gap="xs">
                {data.complianceIssues.map((issue: string, idx: number) => (
                  <Text key={idx} size="sm">• {issue}</Text>
                ))}
              </Stack>
            </Alert>
          )}
          <SimpleGrid cols={{ base: 1, lg: 2 }} spacing="md">
            {data.formatAnalysis && <FormatInfo data={data.formatAnalysis} audioFilePath={data.audioFilePath} />}
            {data.bitrateAnalysis && <BitrateGraph data={data.bitrateAnalysis} durationMs={durationMs} />}
            <Spectrogram folder={folder} songFolder={settings.songFolder} />
          </SimpleGrid>

          {settings.showAdvancedAudioAnalysis &&
            <>
              <Text fw={600}>Advanced Audio Analysis</Text>
              <SimpleGrid cols={{ base: 1, md: 2, lg: 3 }} spacing="md">
                {data.channelAnalysis && <ChannelBalance data={data.channelAnalysis} durationMs={durationMs} />}
                {data.dynamicRangeAnalysis && <DynamicRange data={data.dynamicRangeAnalysis} durationMs={durationMs} />}
                <FrequencyAnalysis data={frequencyData} isLoading={frequencyLoading} />
              </SimpleGrid>
            </>
          }
        </Flex>
      )}
    </Box>
  );
}

export default AudioOverview;

