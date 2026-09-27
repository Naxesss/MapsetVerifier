import {
  Text,
  Badge,
  Group,
  useMantineTheme,
  Stack,
  SimpleGrid,
  List,
  ThemeIcon,
  Tooltip,
  Box,
} from '@mantine/core';
import { IconCheck, IconX, IconAlertTriangle } from '@tabler/icons-react';
import BitrateProgressIndicator from './BitrateProgressIndicator.tsx';
import { BitrateAnalysisResult, FormatAnalysisResult } from '../../../Types';
import { InfoIconTooltip } from '../../common/InfoIconTooltip.tsx';
import SectionCard from '../../common/SectionCard.tsx';

interface FormatInfoProps {
  data: FormatAnalysisResult;
  audioFilePath: string;
  bitrateData?: BitrateAnalysisResult | null;
}

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

function FormatInfo({ data, audioFilePath, bitrateData }: FormatInfoProps) {
  const theme = useMantineTheme();

  // Check if sample rate exceeds 48 kHz
  const sampleRateExceeds48kHz = data.sampleRate > 48000;

  // Check format compliance (MP3 or Ogg Vorbis)
  const isValidFormat = data.format.toLowerCase() === 'mp3' || data.format.toLowerCase() === 'ogg';

  return (
    <SectionCard
      title="Format"
      info="Format information describes the technical properties of an audio file that define how it is stored and played back."
      actions={
        <>
          <Badge color={getBadgeColor(data.badgeType)}>{data.format}</Badge>
          <Badge color={data.isCompliant ? 'green' : 'red'}>
            {data.isCompliant ? 'Compliant' : 'Non-compliant'}
          </Badge>
        </>
      }
    >
      <SimpleGrid cols={3} mb="md" style={{ gridTemplateColumns: '1fr 1fr 1fr' }}>
        <Stack gap="2xs">
          <Text size="xs" c="dimmed">
            File name
          </Text>
          <Text fw={500}>{audioFilePath}</Text>
        </Stack>
        <Stack gap="2xs">
          <Text size="xs" c="dimmed">
            Duration
          </Text>
          <Text fw={500}>{data.durationFormatted}</Text>
        </Stack>
        <Stack gap="2xs">
          <Text size="xs" c="dimmed">
            File size
          </Text>
          <Text fw={500}>{data.fileSizeFormatted}</Text>
        </Stack>
        <Stack gap="2xs">
          <Text size="xs" c="dimmed">
            Channels
          </Text>
          <Text fw={500}>
            {data.channels === 1 ? 'Mono' : data.channels === 2 ? 'Stereo' : `${data.channels}ch`}
          </Text>
        </Stack>
        <Stack gap="2xs">
          <Text size="xs" c="dimmed">
            Sample rate
          </Text>
          <Text fw={500}>{(data.sampleRate / 1000).toFixed(1)} kHz</Text>
          {sampleRateExceeds48kHz && (
            <Text size="xs" c="red.4">
              Exceeds 48 kHz limit
            </Text>
          )}
        </Stack>
        <Stack gap="2xs">
          <Group gap="xs" align="center">
            <Text size="xs" c="dimmed">
              Codec
            </Text>
            {!isValidFormat && (
              <Tooltip label="Must be MP3 or Ogg Vorbis">
                <IconAlertTriangle
                  size={12}
                  style={{ color: theme.colors.red[5], cursor: 'help' }}
                />
              </Tooltip>
            )}
          </Group>
          <Text fw={500} c={isValidFormat ? 'white' : 'red.4'}>
            {data.codec}
          </Text>
        </Stack>
      </SimpleGrid>

      {bitrateData && (
        <>
          <Group justify="space-between" mb="xs">
            <Group gap="xs">
              <Text fw={600} size="sm">
                Bitrate
              </Text>
              <InfoIconTooltip
                label="Bitrate represents the amount of data used per second of audio. Higher bitrates generally preserve more detail but result in larger file sizes."
                multiline
                w={250}
                iconSize={14}
              />
            </Group>
            <Group gap="xs">
              <Badge color={bitrateData.isCompliant ? 'green' : 'red'}>
                {bitrateData.isCompliant ? 'Compliant' : 'Non-compliant'}
              </Badge>
              {bitrateData.isVbr && <Badge color="blue">VBR</Badge>}
            </Group>
          </Group>
          <Box p="sm" mb="md" bg={theme.colors.dark[6]} style={{ borderRadius: theme.radius.sm }}>
            <BitrateProgressIndicator bitrateData={bitrateData} />
          </Box>
        </>
      )}

      {/* Format Requirements Summary */}
      <Box p="xs" mb="md" bg={theme.colors.dark[6]} style={{ borderRadius: theme.radius.sm }}>
        <Text size="xs" fw={500} c="dimmed" mb="xs">
          Ranking requirements
        </Text>
        <Stack gap="xs">
          <Group gap="xs">
            <ThemeIcon size="xs" color={isValidFormat ? 'green' : 'red'} variant="light">
              {isValidFormat ? <IconCheck size={12} /> : <IconX size={12} />}
            </ThemeIcon>
            <Text size="xs" c={isValidFormat ? 'green.4' : 'red.4'}>
              Format is MP3 or Ogg Vorbis
            </Text>
          </Group>
          <Group gap="xs">
            <ThemeIcon size="xs" color={!sampleRateExceeds48kHz ? 'green' : 'red'} variant="light">
              {!sampleRateExceeds48kHz ? <IconCheck size={12} /> : <IconX size={12} />}
            </ThemeIcon>
            <Text size="xs" c={!sampleRateExceeds48kHz ? 'green.4' : 'red.4'}>
              Sample rate 48 kHz or below
            </Text>
          </Group>
        </Stack>
      </Box>

      {data.complianceIssues?.length > 0 && (
        <Stack gap="xs">
          <Text size="sm" fw={500} c="red.4">
            Compliance issues
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
            {data.complianceIssues.map((issue: string, idx: number) => (
              <List.Item key={idx}>{issue}</List.Item>
            ))}
          </List>
        </Stack>
      )}
    </SectionCard>
  );
}

export default FormatInfo;
