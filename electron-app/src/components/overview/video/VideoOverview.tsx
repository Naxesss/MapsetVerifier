import { Button, Stack, Text } from '@mantine/core';
import { IconExternalLink, IconVideoOff } from '@tabler/icons-react';
import FileCard, { WideStat } from '../resources/FileCard.tsx';
import { useVideoAnalysis } from './hooks/useVideoAnalysis';
import VideoPreview from './VideoPreview';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import { buildBeatmapFolderPath } from '../../../utils/buildBeatmapFolderPath.ts';
import { openPathOrNotify } from '../../../utils/notify.tsx';
import EmptyState from '../../common/EmptyState.tsx';
import { StatField } from '../../common/StatField.tsx';
import AnalysisTab from '../AnalysisTab.tsx';
import UsedByField from '../UsedByField.tsx';
import type { VideoAnalysisEntry } from '../../../Types';

interface VideoOverviewProps {
  /** The video picked on Resources. */
  fileName: string;
}

function formatFrameRate(frameRate: number | null): string {
  if (!frameRate) return 'Unknown';
  return `${Number(frameRate.toFixed(3))} FPS`;
}

function formatBitrate(kbps: number | null): string {
  if (!kbps) return 'Unknown';
  if (kbps >= 1000) return `${(kbps / 1000).toFixed(2)} Mbps`;
  return `${Math.round(kbps)} kbps`;
}

function audioTrackNote(video: VideoAnalysisEntry) {
  if (!video.hasAudioTrack || video.audioChannels <= 0) return undefined;
  const channels = video.audioChannels === 1 ? 'Mono' : `${video.audioChannels}ch`;
  const rate =
    video.audioSampleRate > 0 ? ` · ${(video.audioSampleRate / 1000).toFixed(1)} kHz` : '';
  return `${channels}${rate}`;
}

/**
 * One video, laid out like the song and hit sounds: its key numbers, including the offset the
 * difficulties start it at and which ones show it, with the video to play under them. Whether any of it breaks a ranking
 * rule is for Checks to say.
 */
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

  const openInDefaultPlayer = async (video: VideoAnalysisEntry) => {
    const filePath = buildBeatmapFolderPath(beatmapFolderPath, video.fileName);
    if (!filePath) return;
    await openPathOrNotify(filePath, "Couldn't open the video.");
  };

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
          <FileCard
            title="Video"
            info="Plays the video file straight from the mapset folder, so you can check how it lines up with the song."
            actions={
              selected.exists &&
              beatmapFolderPath && (
                <Button
                  size="xs"
                  variant="light"
                  leftSection={<IconExternalLink size={14} />}
                  onClick={() => openInDefaultPlayer(selected)}
                >
                  Open in default player
                </Button>
              )
            }
            footer={
              selected.warnings.length > 0 && (
                <Stack gap="2xs">
                  {selected.warnings.map((warning, idx) => (
                    <Text key={idx} size="xs" c="dimmed">
                      {warning}
                    </Text>
                  ))}
                </Stack>
              )
            }
            stats={
              <>
                <StatField
                  label="Length"
                  value={selected.durationMs > 0 ? selected.durationFormatted : 'Unknown'}
                />
                <StatField
                  label="Resolution"
                  value={selected.width > 0 ? selected.resolution : 'Unknown'}
                />
                <StatField
                  label="Frame rate"
                  value={formatFrameRate(selected.frameRate)}
                  note={selected.isVariableFrameRate ? 'Variable' : undefined}
                />
                <StatField
                  label="Codec"
                  value={selected.videoCodec ?? 'Unknown'}
                  note={selected.videoCodecProfile ?? undefined}
                />
                <StatField
                  label="Bitrate"
                  value={formatBitrate(selected.videoBitrateKbps ?? selected.overallBitrateKbps)}
                  note={selected.videoBitrateKbps ? 'Video track' : 'Whole file'}
                />
                <StatField
                  label="Audio track"
                  value={selected.hasAudioTrack ? (selected.audioCodec ?? 'Present') : 'None'}
                  note={audioTrackNote(selected)}
                />
                <StatField label="Offset" value={`${selected.offsetMs} ms`} />
                <WideStat>
                  <UsedByField difficulties={selected.usedByDifficulties} />
                </WideStat>
              </>
            }
          >
            {beatmapFolderPath && (
              <VideoPreview beatmapFolderPath={beatmapFolderPath} data={selected} />
            )}
          </FileCard>
        )
      }
    </AnalysisTab>
  );
}

export default VideoOverview;
