import { SimpleGrid, SegmentedControl } from '@mantine/core';
import { IconVideoOff } from '@tabler/icons-react';
import { useState } from 'react';
import { useVideoAnalysis } from './hooks/useVideoAnalysis';
import VideoFormatInfo from './VideoFormatInfo';
import VideoPreview from './VideoPreview';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import EmptyState from '../../common/EmptyState.tsx';
import AnalysisTab from '../AnalysisTab.tsx';
import { ComplianceAlert } from '../formatCard.tsx';

function VideoOverview() {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();

  const { data, isLoading, isError, error, beatmapFolderPath } = useVideoAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  const [selectedFileName, setSelectedFileName] = useState<string | null>(null);

  const videos = data?.videos ?? [];

  // Falling back to the first video also covers switching to a set that lacks the selected one.
  const selected = videos.find((video) => video.fileName === selectedFileName) ?? videos[0];

  return (
    <AnalysisTab data={data} isLoading={isLoading} isError={isError} error={error} subject="video">
      {(data) =>
        !selected ? (
          <EmptyState
            icon={IconVideoOff}
            title="No video"
            description="This mapset doesn't use a background video."
          />
        ) : (
          <>
            <ComplianceAlert issues={data.complianceIssues} />

            {videos.length > 1 && (
              <SegmentedControl
                value={selected.fileName}
                onChange={setSelectedFileName}
                data={videos.map((video) => video.fileName)}
              />
            )}

            <SimpleGrid cols={{ base: 1, lg: 2 }} spacing="md">
              <VideoFormatInfo data={selected} />
              {beatmapFolderPath && (
                <VideoPreview beatmapFolderPath={beatmapFolderPath} data={selected} />
              )}
            </SimpleGrid>
          </>
        )
      }
    </AnalysisTab>
  );
}

export default VideoOverview;
