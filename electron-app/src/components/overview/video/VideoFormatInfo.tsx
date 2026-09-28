import { Badge, Box, Group, SimpleGrid, Stack, Text } from '@mantine/core';
import { VideoAnalysisEntry } from '../../../Types';
import SectionCard from '../../common/SectionCard.tsx';
import { StatField } from '../../common/StatField.tsx';
import {
  ComplianceBadge,
  ComplianceIssueList,
  formatBadgeColor,
  RequirementsList,
  RuleLabel,
} from '../formatCard.tsx';

interface VideoFormatInfoProps {
  data: VideoAnalysisEntry;
}

const MAX_WIDTH = 1280;
const MAX_HEIGHT = 720;

function formatFrameRate(frameRate: number | null): string {
  if (!frameRate) return 'Unknown';
  return `${Number(frameRate.toFixed(3))} FPS`;
}

function formatBitrate(kbps: number | null): string {
  if (!kbps) return 'Unknown';
  if (kbps >= 1000) return `${(kbps / 1000).toFixed(2)} Mbps`;
  return `${Math.round(kbps)} kbps`;
}

function VideoFormatInfo({ data }: VideoFormatInfoProps) {
  const resolutionIsValid =
    data.width > 0 && data.width <= MAX_WIDTH && data.height > 0 && data.height <= MAX_HEIGHT;

  return (
    <SectionCard
      title="Format"
      info="Format information describes the technical properties of a video file that define how it is stored and played back."
      actions={
        <>
          <Badge color={formatBadgeColor(data.badgeType)}>{data.container}</Badge>
          <ComplianceBadge compliant={data.isCompliant} />
        </>
      }
    >
      <SimpleGrid cols={3} mb="md" style={{ gridTemplateColumns: '1fr 1fr 1fr' }}>
        <StatField label="File name" value={data.fileName} />
        <StatField
          label={
            <RuleLabel
              label="Resolution"
              brokenRule={
                resolutionIsValid ? undefined : `Must not exceed ${MAX_WIDTH} x ${MAX_HEIGHT}`
              }
            />
          }
          value={data.width > 0 ? data.resolution : 'Unknown'}
          valueColor={resolutionIsValid ? undefined : 'red.4'}
        />
        <StatField
          label="Duration"
          value={data.durationMs > 0 ? data.durationFormatted : 'Unknown'}
        />
        <StatField
          label="Frame rate"
          value={formatFrameRate(data.frameRate)}
          note={data.isVariableFrameRate ? 'Variable frame rate' : undefined}
          noteColor="yellow.4"
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
          valueColor={data.hasAudioTrack ? 'red.4' : undefined}
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

      <RequirementsList
        requirements={[
          { label: `Resolution is ${MAX_WIDTH} x ${MAX_HEIGHT} or below`, met: resolutionIsValid },
          { label: 'No audio track present', met: !data.hasAudioTrack },
        ]}
      />

      {data.usedByDifficulties.length > 0 && (
        <Box mb="md">
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

      <ComplianceIssueList
        issues={data.complianceIssues}
        mb={data.warnings.length > 0 ? 'md' : 0}
      />

      {data.warnings.length > 0 && (
        <Stack gap="2xs">
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
