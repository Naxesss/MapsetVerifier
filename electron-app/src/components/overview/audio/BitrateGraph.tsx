import {
  Text,
  Badge,
  Group,
  Paper,
  useMantineTheme,
  Stack,
  Tooltip,
  SimpleGrid,
} from '@mantine/core';
import { IconAlertTriangle, IconInfoCircle } from '@tabler/icons-react';
import { useMemo } from 'react';
import { BitrateAnalysisResult, BitrateDataPoint } from '../../../Types';
import { formatChartTime } from '../../common/TimeAxis.tsx';

interface BitrateGraphProps {
  data: BitrateAnalysisResult;
}

function BitrateGraph({ data }: BitrateGraphProps) {
  const theme = useMantineTheme();

  // Transform data for Mantine LineChart - sample data for performance
  useMemo(() => {
    if (!data.bitrateOverTime?.length) return [];
    const rawData = data.bitrateOverTime;
    // Sample data if too many points for performance
    const maxPoints = 200;
    const step = Math.max(1, Math.floor(rawData.length / maxPoints));
    const sampled = rawData.filter((_: BitrateDataPoint, i: number) => i % step === 0);
    return sampled.map((point: BitrateDataPoint) => ({
      time: formatChartTime(point.timeMs / 1000),
      bitrate: Math.round(point.bitrate),
    }));
  }, [data.bitrateOverTime]);

  // Check for violations in the bitrate data
  const violations = useMemo(() => {
    if (!data.bitrateOverTime?.length) return { hasViolations: false, aboveMax: 0, belowMin: 0 };
    let aboveMax = 0;
    let belowMin = 0;
    data.bitrateOverTime.forEach((point: BitrateDataPoint) => {
      if (point.bitrate > data.maxAllowedBitrate) aboveMax++;
      if (point.bitrate < data.minAllowedBitrate) belowMin++;
    });
    return {
      hasViolations: aboveMax > 0 || belowMin > 0,
      aboveMax,
      belowMin,
      totalPoints: data.bitrateOverTime.length,
    };
  }, [data.bitrateOverTime, data.maxAllowedBitrate, data.minAllowedBitrate]);

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
      <Stack gap="sm">
        <Group justify="space-between">
          <Group gap="xs">
            <Text fw={600}>Bitrate Information</Text>
            <Tooltip
              label="Bitrate represents the amount of data used per second of audio. Higher bitrates generally preserve more detail but result in larger file sizes."
              multiline
              w={250}
            >
              <IconInfoCircle size={16} style={{ color: theme.colors.gray[6], cursor: 'help' }} />
            </Tooltip>
          </Group>
          <Group gap="xs">
            <Badge color={data.isCompliant ? 'green' : 'red'} variant="light">
              {data.isCompliant ? 'Compliant' : 'Non-Compliant'}
            </Badge>
            {data.isVbr && (
              <Badge color="blue" variant="light">
                VBR
              </Badge>
            )}
          </Group>
        </Group>
        <SimpleGrid cols={3} mb="md" style={{ gridTemplateColumns: '1fr 1fr 1fr' }}>
          <Stack gap={2}>
            <Text size="xs" c="dimmed">
              Average Bitrate
            </Text>
            <Text fw={500}>{data.averageBitrate}</Text>
          </Stack>
          <Stack gap={2}>
            <Text size="xs" c="dimmed">
              Min Allowed Bitrate
            </Text>
            <Text fw={500}>{data.minAllowedBitrate}</Text>
          </Stack>
          <Stack gap={2}>
            <Text size="xs" c="dimmed">
              Max Allowed Bitrate
            </Text>
            <Text fw={500}>{data.maxAllowedBitrate}</Text>
          </Stack>
        </SimpleGrid>

        {/* Compliance Status */}
        {violations.hasViolations && (
          <Group
            gap="xs"
            p="xs"
            bg={theme.colors.dark[6]}
            style={{ borderRadius: theme.radius.sm }}
          >
            <IconAlertTriangle size={16} color={theme.colors.red[5]} />
            <Text size="xs" c="red.4">
              {violations.aboveMax > 0 && `${violations.aboveMax} samples exceed max threshold`}
              {violations.aboveMax > 0 && violations.belowMin > 0 && ' • '}
              {violations.belowMin > 0 && `${violations.belowMin} samples below min threshold`}
            </Text>
          </Group>
        )}
      </Stack>
    </Paper>
  );
}

export default BitrateGraph;
