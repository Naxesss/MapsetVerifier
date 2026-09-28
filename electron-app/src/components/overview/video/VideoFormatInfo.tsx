import { Badge, Box, Group, SimpleGrid, Stack, Text } from '@mantine/core';
import { VideoAnalysisEntry } from '../../../Types';
import SectionCard from '../../common/SectionCard.tsx';
import { StatField } from '../../common/StatField.tsx';

interface VideoFormatInfoProps {
  data: VideoAnalysisEntry;
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

/**
 * What the video file is: container, resolution, codec, frame rate, audio track and more. Whether
 * any of it breaks a ranking rule is for Checks to say.
 */
function VideoFormatInfo({ data }: VideoFormatInfoProps) {
  return (
    <SectionCard
      title="Format"
      info="Format information describes the technical properties of a video file that define how it is stored and played back."
      actions={<Badge>{data.container}</Badge>}
    >
      <SimpleGrid cols={3} style={{ gridTemplateColumns: '1fr 1fr 1fr' }}>
        <StatField label="File name" value={data.fileName} />
        <StatField label="Resolution" value={data.width > 0 ? data.resolution : 'Unknown'} />
        <StatField
          label="Duration"
          value={data.durationMs > 0 ? data.durationFormatted : 'Unknown'}
        />
        <StatField
          label="Frame rate"
          value={formatFrameRate(data.frameRate)}
          note={data.isVariableFrameRate ? 'Variable frame rate' : undefined}
        />
        <StatField
          label="Codec"
          value={data.videoCodec ?? 'Unknown'}
          note={data.videoCodecProfile ?? undefined}
        />
        <StatField
          label="Bitrate"
          value={formatBitrate(data.videoBitrateKbps ?? data.overallBitrateKbps)}
          note={data.videoBitrateKbps ? 'Video track' : 'Whole file'}
        />
        <StatField label="File size" value={data.fileSizeFormatted} />
        <StatField label="Offset" value={`${data.offsetMs} ms`} />
        <StatField
          label="Audio track"
          value={data.hasAudioTrack ? (data.audioCodec ?? 'Present') : 'None'}
          note={
            data.hasAudioTrack && data.audioChannels > 0
              ? `${data.audioChannels === 1 ? 'Mono' : `${data.audioChannels}ch`}${
                  data.audioSampleRate > 0
                    ? ` · ${(data.audioSampleRate / 1000).toFixed(1)} kHz`
                    : ''
                }`
              : undefined
          }
        />
      </SimpleGrid>

      {data.usedByDifficulties.length > 0 && (
        <Box mt="md">
          <StatField
            label="Used by"
            value={
              <Group gap="xs">
                {data.usedByDifficulties.map((difficulty) => (
                  <Badge key={difficulty}>{difficulty}</Badge>
                ))}
              </Group>
            }
          />
        </Box>
      )}

      {data.warnings.length > 0 && (
        <Stack gap="2xs" mt="md">
          {data.warnings.map((warning, idx) => (
            <Text key={idx} size="xs" c="dimmed">
              {warning}
            </Text>
          ))}
        </Stack>
      )}
    </SectionCard>
  );
}

export default VideoFormatInfo;
