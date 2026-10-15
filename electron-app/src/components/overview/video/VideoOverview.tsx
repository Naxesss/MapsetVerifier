import {
  Alert,
  Text,
  Box,
  Flex,
  Stack,
  SimpleGrid,
  LoadingOverlay,
  SegmentedControl,
} from '@mantine/core';
import {
  IconAlertCircle,
  IconAlertTriangle,
  IconRulerMeasure,
  IconVideoOff,
} from '@tabler/icons-react';
import { useState } from 'react';
import { useVideoAnalysis } from './hooks/useVideoAnalysis';
import VideoFormatInfo from './VideoFormatInfo';
import VideoPreview from './VideoPreview';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import NoBeatmapsetDisplay from '../../common/NoBeatmapsetDisplay.tsx';
import StackTraceMessage from '../../common/StackTraceMessage.tsx';

function VideoOverview() {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();

  const { data, isLoading, isFetching, isError, error, beatmapFolderPath } = useVideoAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  const [selectedFileName, setSelectedFileName] = useState<string | null>(null);

  const videos = data?.videos ?? [];

  if (!folder) {
    return <NoBeatmapsetDisplay />;
  }

  // Falling back to the first video also covers switching to a set that lacks the selected one.
  const selected = videos.find((video) => video.fileName === selectedFileName) ?? videos[0];

  return (
    <Box>
      <LoadingOverlay
        visible={isLoading || isFetching}
        zIndex={1000}
        overlayProps={{ radius: 'sm', blur: 2 }}
      />
      {isError && (
        <Flex p="md">
          <Alert icon={<IconAlertCircle />} color="red" title="Error analyzing video">
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

      {data && data.success && videos.length === 0 && (
        <Flex p="md">
          <Alert icon={<IconVideoOff />} color="gray" title="No video">
            <Text size="sm">This beatmapset does not use a background video.</Text>
          </Alert>
        </Flex>
      )}

      {data && data.success && selected && (
        <Flex gap="md" p="md" direction="column">
          {data.complianceIssues.length > 0 && (
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

          {videos.length > 1 && (
            <SegmentedControl
              value={selected.fileName}
              onChange={setSelectedFileName}
              data={videos.map((video) => video.fileName)}
              size="xs"
            />
          )}

          <SimpleGrid cols={{ base: 1, lg: 2 }} spacing="md">
            <VideoFormatInfo data={selected} />
            {beatmapFolderPath && (
              <VideoPreview beatmapFolderPath={beatmapFolderPath} data={selected} />
            )}
          </SimpleGrid>
        </Flex>
      )}
    </Box>
  );
}

export default VideoOverview;
