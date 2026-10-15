import { Alert, Box, Button, Group, Paper, Text, useMantineTheme } from '@mantine/core';
import { IconExternalLink, IconVideoOff } from '@tabler/icons-react';
import { useState } from 'react';
import { VideoAnalysisEntry } from '../../../Types';
import {
  buildBeatmapFolderPath,
  buildBeatmapVideoUrl,
} from '../../../utils/buildBeatmapFolderPath.ts';
import { InfoIconTooltip } from '../../common/InfoIconTooltip.tsx';

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

    try {
      const err = await window.electronAPI?.shell.openPath(filePath);
      if (err) throw new Error(err);
    } catch (e) {
      console.error('Failed to open video:', e);
      alert('Failed to open the video. See console for details.');
    }
  };

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
      <Group justify="space-between" mb="md">
        <Group gap="xs">
          <Text fw={600}>Preview</Text>
          <InfoIconTooltip
            label="Plays the video file straight from the beatmap folder, so you can check how it lines up with the song."
            multiline
            w={250}
          />
        </Group>
        {data.exists && (
          <Button
            size="xs"
            variant="default"
            leftSection={<IconExternalLink size={14} />}
            onClick={openInDefaultPlayer}
          >
            Open in default player
          </Button>
        )}
      </Group>

      {unsupported ? (
        <Alert icon={<IconVideoOff />} color="gray">
          <Text size="sm">
            {!data.exists
              ? 'The video file is missing, so there is nothing to preview.'
              : `${data.container} files cannot be played here, since the browser has no ${data.container} demuxer. Open it in your default player instead.`}
          </Text>
        </Alert>
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
    </Paper>
  );
}

export default VideoPreview;
