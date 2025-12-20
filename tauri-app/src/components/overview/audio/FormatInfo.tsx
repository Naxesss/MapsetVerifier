import { Text, Badge, Group, Paper, useMantineTheme, Stack, SimpleGrid, List, ThemeIcon, Tooltip, Box } from '@mantine/core';
import { IconCheck, IconX, IconAlertTriangle, IconInfoCircle } from '@tabler/icons-react';
import { FormatAnalysisResult } from '../../Types';

interface FormatInfoProps {
  data: FormatAnalysisResult;
  audioFilePath: string;
}

function getBadgeColor(badgeType: string): string {
  switch (badgeType) {
    case 'success': return 'green';
    case 'warning': return 'yellow';
    case 'error': return 'red';
    default: return 'gray';
  }
}

function FormatInfo({ data, audioFilePath }: FormatInfoProps) {
  const theme = useMantineTheme();

  // Check if sample rate exceeds 48 kHz
  const sampleRateExceeds48kHz = data.sampleRate > 48000;

  // Check format compliance (MP3 or Ogg Vorbis)
  const isValidFormat = data.format.toLowerCase() === 'mp3' || data.format.toLowerCase() === 'ogg';

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
      <Group justify="space-between" mb="md">
        <Group gap="xs">
          <Text fw={600}>Format Information</Text>
          <Tooltip
            label="Audio must be MP3 (.mp3) or Ogg Vorbis (.ogg) format with sample rate ≤ 48 kHz"
            multiline
            w={250}
          >
            <IconInfoCircle size={16} style={{ color: theme.colors.gray[6], cursor: 'help' }} />
          </Tooltip>
        </Group>
        <Group gap="xs">
          <Badge color={getBadgeColor(data.badgeType)} variant="light" size="lg">
            {data.format}
          </Badge>
          <Badge color={data.isCompliant ? 'green' : 'red'} variant="light">
            {data.isCompliant ? 'Compliant' : 'Non-Compliant'}
          </Badge>
        </Group>
      </Group>

      <SimpleGrid cols={3} mb="md" style={{ gridTemplateColumns: '1fr 1fr 1fr' }}>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">File Name</Text>
          <Text fw={500}>{audioFilePath}</Text>
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">Duration</Text>
          <Text fw={500}>{data.durationFormatted}</Text>
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">File Size</Text>
          <Text fw={500}>{data.fileSizeFormatted}</Text>
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">Channels</Text>
          <Text fw={500}>{data.channels === 1 ? 'Mono' : data.channels === 2 ? 'Stereo' : `${data.channels}ch`}</Text>
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">Sample Rate</Text>
          <Text fw={500}>{(data.sampleRate / 1000).toFixed(1)} kHz</Text>
          {sampleRateExceeds48kHz && (
            <Text size="xs" c="red.4">Exceeds 48 kHz limit</Text>
          )}
        </Stack>
        <Stack gap={2}>
          <Group gap={4} align="center">
            <Text size="xs" c="dimmed">Codec</Text>
            {!isValidFormat && (
              <Tooltip label="Must be MP3 or Ogg Vorbis">
                <IconAlertTriangle size={12} style={{ color: theme.colors.red[5], cursor: 'help' }} />
              </Tooltip>
            )}
          </Group>
          <Text fw={500} c={isValidFormat ? 'white' : 'red.4'}>{data.codec}</Text>
        </Stack>
      </SimpleGrid>

      {/* Format Requirements Summary */}
      <Box p="xs" mb="md" bg={theme.colors.dark[6]} style={{ borderRadius: theme.radius.sm }}>
        <Text size="xs" fw={500} c="dimmed" mb={4}>Ranking Requirements:</Text>
        <Stack gap={4}>
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
              Sample Rate 48 kHz or below
            </Text>
          </Group>
        </Stack>
      </Box>

      {data.complianceIssues?.length > 0 && (
        <Stack gap="xs">
          <Text size="sm" fw={500} c="red.4">Compliance Issues:</Text>
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
    </Paper>
  );
}

export default FormatInfo;

