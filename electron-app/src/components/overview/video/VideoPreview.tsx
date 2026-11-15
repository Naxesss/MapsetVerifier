import { Box, useMantineTheme } from '@mantine/core';
import { IconVideoOff } from '@tabler/icons-react';
import { useState } from 'react';
import { VideoAnalysisEntry } from '../../../Types';
import { buildBeatmapVideoUrl } from '../../../utils/buildBeatmapFolderPath.ts';
import EmptyState from '../../common/EmptyState.tsx';

interface VideoPreviewProps {
  beatmapFolderPath: string;
  data: VideoAnalysisEntry;
}

/** The video file played straight from the mapset folder, or why it can't be. */
function VideoPreview({ beatmapFolderPath, data }: VideoPreviewProps) {
  const theme = useMantineTheme();
  // Keyed by url so a newly selected video gets a fresh chance to play.
  const [failedUrl, setFailedUrl] = useState<string | null>(null);

  const url = buildBeatmapVideoUrl(beatmapFolderPath, data.fileName);
  const unsupported = !data.exists || !data.canPreview || failedUrl === url;

  if (unsupported) {
    return (
      <EmptyState
        icon={IconVideoOff}
        title={data.exists ? "Can't preview this video" : 'Video file missing'}
        description={
          data.exists
            ? `${data.container} files can't be played here. Open it in your default player instead.`
            : 'The video file is missing, so there is nothing to preview.'
        }
      />
    );
  }

  return (
    <Box style={{ borderRadius: theme.radius.sm, overflow: 'hidden' }} bg={theme.colors.dark[7]}>
      <video
        key={url}
        src={url}
        controls
        preload="metadata"
        onError={() => setFailedUrl(url)}
        style={{ width: '100%', display: 'block', maxHeight: 420 }}
      />
    </Box>
  );
}

export default VideoPreview;
