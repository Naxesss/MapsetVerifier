import {
  Text,
  Badge,
  Group,
  Paper,
  useMantineTheme,
  Stack,
  SimpleGrid,
  List,
  ThemeIcon,
  Tooltip,
  Box,
} from '@mantine/core';
import { IconCheck, IconX, IconAlertTriangle } from '@tabler/icons-react';
import { VideoAnalysisEntry } from '../../../Types';
import { InfoIconTooltip } from '../../common/InfoIconTooltip.tsx';

interface VideoFormatInfoProps {
  data: VideoAnalysisEntry;
}

const MAX_WIDTH = 1280;
const MAX_HEIGHT = 720;

function getBadgeColor(badgeType: string): string {
  switch (badgeType) {
    case 'success':
      return 'green';
    case 'warning':
      return 'yellow';
    case 'error':
      return 'red';
    default:
      return 'gray';
  }
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

function VideoFormatInfo({ data }: VideoFormatInfoProps) {
  const theme = useMantineTheme();

  const resolutionIsValid =
    data.width > 0 && data.width <= MAX_WIDTH && data.height > 0 && data.height <= MAX_HEIGHT;

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
      <Group justify="space-between" mb="md">
        <Group gap="xs">
          <Text fw={600}>Format Information</Text>
          <InfoIconTooltip
            label="Format information describes the technical properties of a video file that define how it is stored and played back."
            multiline
            w={250}
          />
        </Group>
        <Group gap="xs">
          <Badge color={getBadgeColor(data.badgeType)} variant="light">
            {data.container}
          </Badge>
          <Badge color={data.isCompliant ? 'green' : 'red'} variant="light">
            {data.isCompliant ? 'Compliant' : 'Non-Compliant'}
          </Badge>
        </Group>
      </Group>

      <SimpleGrid cols={3} mb="md" style={{ gridTemplateColumns: '1fr 1fr 1fr' }}>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">
            File Name
          </Text>
          <Text fw={500}>{data.fileName}</Text>
        </Stack>
        <Stack gap={2}>
          <Group gap={4} align="center">
            <Text size="xs" c="dimmed">
              Resolution
            </Text>
            {!resolutionIsValid && (
              <Tooltip label={`Must not exceed ${MAX_WIDTH} x ${MAX_HEIGHT}`}>
                <IconAlertTriangle
                  size={12}
                  style={{ color: theme.colors.red[5], cursor: 'help' }}
                />
              </Tooltip>
            )}
          </Group>
          <Text fw={500} c={resolutionIsValid ? 'white' : 'red.4'}>
            {data.width > 0 ? data.resolution : 'Unknown'}
          </Text>
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">
            Duration
          </Text>
          <Text fw={500}>{data.durationMs > 0 ? data.durationFormatted : 'Unknown'}</Text>
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">
            Frame Rate
          </Text>
          <Text fw={500}>{formatFrameRate(data.frameRate)}</Text>
          {data.isVariableFrameRate && (
            <Text size="xs" c="yellow.4">
              Variable frame rate
            </Text>
          )}
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">
            Codec
          </Text>
          <Text fw={500}>{data.videoCodec ?? 'Unknown'}</Text>
          {data.videoCodecProfile && (
            <Text size="xs" c="dimmed">
              {data.videoCodecProfile}
            </Text>
          )}
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">
            Bitrate
          </Text>
          <Text fw={500}>{formatBitrate(data.videoBitrateKbps ?? data.overallBitrateKbps)}</Text>
          <Text size="xs" c="dimmed">
            {data.videoBitrateKbps ? 'Video track' : 'Whole file'}
          </Text>
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">
            File Size
          </Text>
          <Text fw={500}>{data.fileSizeFormatted}</Text>
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">
            Offset
          </Text>
          <Text fw={500}>{data.offsetMs} ms</Text>
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">
            Audio Track
          </Text>
          <Text fw={500} c={data.hasAudioTrack ? 'red.4' : 'white'}>
            {data.hasAudioTrack ? (data.audioCodec ?? 'Present') : 'None'}
          </Text>
          {data.hasAudioTrack && data.audioChannels > 0 && (
            <Text size="xs" c="dimmed">
              {data.audioChannels === 1 ? 'Mono' : `${data.audioChannels}ch`}
              {data.audioSampleRate > 0 && ` · ${(data.audioSampleRate / 1000).toFixed(1)} kHz`}
            </Text>
          )}
        </Stack>
      </SimpleGrid>

      <Box p="xs" mb="md" bg={theme.colors.dark[6]} style={{ borderRadius: theme.radius.sm }}>
        <Text size="xs" fw={500} c="dimmed" mb={4}>
          Ranking Requirements:
        </Text>
        <Stack gap={4}>
          <Group gap="xs">
            <ThemeIcon size="xs" color={resolutionIsValid ? 'green' : 'red'} variant="light">
              {resolutionIsValid ? <IconCheck size={12} /> : <IconX size={12} />}
            </ThemeIcon>
            <Text size="xs" c={resolutionIsValid ? 'green.4' : 'red.4'}>
              Resolution is {MAX_WIDTH} x {MAX_HEIGHT} or below
            </Text>
          </Group>
          <Group gap="xs">
            <ThemeIcon size="xs" color={data.hasAudioTrack ? 'red' : 'green'} variant="light">
              {data.hasAudioTrack ? <IconX size={12} /> : <IconCheck size={12} />}
            </ThemeIcon>
            <Text size="xs" c={data.hasAudioTrack ? 'red.4' : 'green.4'}>
              No audio track present
            </Text>
          </Group>
        </Stack>
      </Box>

      {data.usedByDifficulties.length > 0 && (
        <Stack gap={2} mb="md">
          <Text size="xs" c="dimmed">
            Used By
          </Text>
          <Group gap="xs">
            {data.usedByDifficulties.map((difficulty) => (
              <Badge key={difficulty} size="xs" variant="light">
                {difficulty}
              </Badge>
            ))}
          </Group>
        </Stack>
      )}

      {data.complianceIssues.length > 0 && (
        <Stack gap="xs" mb={data.warnings.length > 0 ? 'md' : 0}>
          <Text size="sm" fw={500} c="red.4">
            Compliance Issues:
          </Text>
          <List
            size="sm"
            spacing="xs"
            icon={
              <ThemeIcon color="red" size="sm" variant="light">
                <IconX size={14} />
              </ThemeIcon>
            }
          >
            {data.complianceIssues.map((issue, idx) => (
              <List.Item key={idx}>{issue}</List.Item>
            ))}
          </List>
        </Stack>
      )}

      {data.warnings.length > 0 && (
        <Stack gap={2}>
          {data.warnings.map((warning, idx) => (
            <Text key={idx} size="xs" c="dimmed">
              {warning}
            </Text>
          ))}
        </Stack>
      )}
    </Paper>
  );
}

export default VideoFormatInfo;
