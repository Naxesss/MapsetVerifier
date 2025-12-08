import { Text, Badge, Group, Paper, useMantineTheme, Stack, SimpleGrid, List, ThemeIcon } from '@mantine/core';
import { IconCheck, IconX, IconAlertTriangle } from '@tabler/icons-react';
import { FormatAnalysisResult } from '../../Types';

interface FormatInfoProps {
  data: FormatAnalysisResult;
}

function getBadgeColor(badgeType: string): string {
  switch (badgeType) {
    case 'success': return 'green';
    case 'warning': return 'yellow';
    case 'error': return 'red';
    default: return 'gray';
  }
}

function FormatInfo({ data }: FormatInfoProps) {
  const theme = useMantineTheme();

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[7]}>
      <Group justify="space-between" mb="md">
        <Text fw={600}>Format Information</Text>
        <Group gap="xs">
          <Badge color={getBadgeColor(data.badgeType)} variant="light" size="lg">
            {data.format}
          </Badge>
          <Badge color={data.isCompliant ? 'green' : 'red'} variant="light">
            {data.isCompliant ? 'Compliant' : 'Non-Compliant'}
          </Badge>
        </Group>
      </Group>
      
      <SimpleGrid cols={3} mb="md">
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
          <Group gap={4}>
            <Text fw={500}>{(data.sampleRate / 1000).toFixed(1)} kHz</Text>
            {data.isStandardSampleRate ? (
              <ThemeIcon size="xs" color="green" variant="light"><IconCheck size={12} /></ThemeIcon>
            ) : (
              <ThemeIcon size="xs" color="yellow" variant="light"><IconAlertTriangle size={12} /></ThemeIcon>
            )}
          </Group>
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">Bit Depth</Text>
          <Text fw={500}>{data.bitDepth}-bit</Text>
        </Stack>
        <Stack gap={2}>
          <Text size="xs" c="dimmed">Codec</Text>
          <Text fw={500}>{data.codec}</Text>
        </Stack>
      </SimpleGrid>

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

      {data.isCompliant && data.complianceIssues?.length === 0 && (
        <Group gap="xs">
          <ThemeIcon color="green" size="sm" variant="light">
            <IconCheck size={14} />
          </ThemeIcon>
          <Text size="sm" c="green.4">All format requirements met for ranking</Text>
        </Group>
      )}
    </Paper>
  );
}

export default FormatInfo;

