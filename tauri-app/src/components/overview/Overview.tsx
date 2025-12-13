import { Alert, Text, Box, useMantineTheme, Flex, LoadingOverlay, Button, Group, Stack, SimpleGrid } from '@mantine/core';
import { IconRefresh } from '@tabler/icons-react';
import { useBeatmap } from '../../context/BeatmapContext';
import { useSettings } from '../../context/SettingsContext';
import { useBeatmapBackground } from '../checks/hooks/useBeatmapBackground';
import BeatmapHeader from '../common/BeatmapHeader';
import { useAudioAnalysis, useSpectrogram, useFrequencyAnalysis } from './hooks/useAudioAnalysis';
import BitrateGraph from './BitrateGraph';
import ChannelBalance from './ChannelBalance';
import DynamicRange from './DynamicRange';
import FormatInfo from './FormatInfo';
import Spectrogram from './Spectrogram';
import FrequencyAnalysis from './FrequencyAnalysis';

function Overview() {
  const theme = useMantineTheme();
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();

  const { data, isLoading, isError, error, refetch } = useAudioAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  const { data: spectrogramData, isLoading: spectrogramLoading } = useSpectrogram({
    folder,
    songFolder: settings.songFolder,
  });

  const { data: frequencyData, isLoading: frequencyLoading } = useFrequencyAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  const { bgUrl } = useBeatmapBackground(folder);

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
      <LoadingOverlay visible={isLoading} zIndex={1000} overlayProps={{ radius: "sm", blur: 2 }} />
      <BeatmapHeader bgUrl={bgUrl}>
        <Group gap="sm">
          <Group p="xs" gap="xs" bg={theme.colors.dark[8]} style={{ borderRadius: theme.radius.md }}>
            <Button
              variant="default"
              size="xs"
              leftSection={<IconRefresh size={16} />}
              onClick={() => refetch()}
            >
              Refresh Analysis
            </Button>
          </Group>
        </Group>
      </BeatmapHeader>

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
                {data.complianceIssues.map((issue, idx) => (
                  <Text key={idx} size="sm">• {issue}</Text>
                ))}
              </Stack>
            </Alert>
          )}
          <SimpleGrid cols={{ base: 1, md: 2, lg: 3 }} spacing="md">
            {/*{data.formatAnalysis && <FormatInfo data={data.formatAnalysis} audioFilePath={data.audioFilePath} />}*/}
            {/*{data.bitrateAnalysis && <BitrateGraph data={data.bitrateAnalysis} />}*/}
            {/*{data.channelAnalysis && <ChannelBalance data={data.channelAnalysis} />}*/}
            {/*{data.dynamicRangeAnalysis && <DynamicRange data={data.dynamicRangeAnalysis} />}*/}
            <Spectrogram data={spectrogramData} isLoading={spectrogramLoading} />
            {/*<FrequencyAnalysis data={frequencyData} isLoading={frequencyLoading} />*/}
          </SimpleGrid>
        </Flex>
      )}
    </Box>
  );
}

export default Overview;

