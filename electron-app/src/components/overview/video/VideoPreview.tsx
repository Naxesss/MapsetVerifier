import { Box, Button, useMantineTheme } from '@mantine/core';
import { IconExternalLink, IconVideoOff } from '@tabler/icons-react';
import { useState } from 'react';
import { VideoAnalysisEntry } from '../../../Types';
import {
  buildBeatmapFolderPath,
  buildBeatmapVideoUrl,
} from '../../../utils/buildBeatmapFolderPath.ts';
import { openPathOrNotify } from '../../../utils/notify.tsx';
import EmptyState from '../../common/EmptyState.tsx';
import SectionCard from '../../common/SectionCard.tsx';

interface VideoPreviewProps {
  beatmapFolderPath: string;
  data: VideoAnalysisEntry;
}

function VideoPreview({ beatmapFolderPath, data }: VideoPreviewProps) {
  const theme = useMantineTheme();
  // Keyed by url so a newly selected video gets a fresh chance to play.
  const [failedUrl, setFailedUrl] = useState<string | null>(null);

  const url = buildBeatmapVideoUrl(beatmapFolderPath, data.fileName);
  const unsupported = !data.exists || !data.canPreview || failedUrl === url;

  const openInDefaultPlayer = async () => {
    const filePath = buildBeatmapFolderPath(beatmapFolderPath, data.fileName);
    if (!filePath) return;

    await openPathOrNotify(filePath, "Couldn't open the video.");
  };

  return (
    <SectionCard
      title="Preview"
      info="Plays the video file straight from the mapset folder, so you can check how it lines up with the song."
      actions={
        data.exists && (
          <Button
            size="xs"
            variant="light"
            leftSection={<IconExternalLink size={14} />}
            onClick={openInDefaultPlayer}
          >
            Open in default player
          </Button>
        )
      }
    >
      {unsupported ? (
        <EmptyState
          icon={IconVideoOff}
          title={data.exists ? "Can't preview this video" : 'Video file missing'}
          description={
            data.exists
              ? `${data.container} files can't be played here. Open it in your default player instead.`
              : 'The video file is missing, so there is nothing to preview.'
          }
        />
      ) : (
        <Box
          style={{ borderRadius: theme.radius.sm, overflow: 'hidden' }}
          bg={theme.colors.dark[7]}
        >
          <video
            key={url}
            src={url}
            controls
            preload="metadata"
            onError={() => setFailedUrl(url)}
            style={{ width: '100%', display: 'block', maxHeight: 420 }}
          />
        </Box>
      )}
    </SectionCard>
  );
}

export default VideoPreview;
