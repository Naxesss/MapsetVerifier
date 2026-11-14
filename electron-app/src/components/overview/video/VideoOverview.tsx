import { SimpleGrid } from '@mantine/core';
import { IconVideoOff } from '@tabler/icons-react';
import { useVideoAnalysis } from './hooks/useVideoAnalysis';
import VideoFormatInfo from './VideoFormatInfo';
import VideoPreview from './VideoPreview';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import EmptyState from '../../common/EmptyState.tsx';
import AnalysisTab from '../AnalysisTab.tsx';

interface VideoOverviewProps {
  /** The video picked on Resources. */
  fileName: string;
}

/** One video's format and a preview to play it, as a file's details on Resources. */
function VideoOverview({ fileName }: VideoOverviewProps) {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();

  const { data, isLoading, isError, error, beatmapFolderPath } = useVideoAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  const selected = data?.videos.find(
    (video) => video.fileName.toLowerCase() === fileName.toLowerCase()
  );

  return (
    <AnalysisTab
      data={data}
      isLoading={isLoading}
      isError={isError}
      error={error}
      subject="video"
      padded={false}
    >
      {() =>
        !selected ? (
          <EmptyState
            icon={IconVideoOff}
            title="Video not found"
            description="This video couldn't be read from the mapset folder."
          />
        ) : (
          <SimpleGrid cols={{ base: 1, lg: 2 }} spacing="md">
            <VideoFormatInfo data={selected} />
            {beatmapFolderPath && (
              <VideoPreview beatmapFolderPath={beatmapFolderPath} data={selected} />
            )}
          </SimpleGrid>
        )
      }
    </AnalysisTab>
  );
}

export default VideoOverview;
