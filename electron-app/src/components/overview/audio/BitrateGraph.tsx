import { Text, Badge, Group, useMantineTheme, Stack, SimpleGrid } from '@mantine/core';
import { IconAlertTriangle } from '@tabler/icons-react';
import { useMemo } from 'react';
import { BitrateAnalysisResult, BitrateDataPoint } from '../../../Types';
import SectionCard from '../../common/SectionCard.tsx';
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
    <SectionCard
      title="Bitrate"
      info="Bitrate represents the amount of data used per second of audio. Higher bitrates generally preserve more detail but result in larger file sizes."
      actions={
        <>
          <Badge color={data.isCompliant ? 'green' : 'red'}>
            {data.isCompliant ? 'Compliant' : 'Non-compliant'}
          </Badge>
          {data.isVbr && <Badge color="blue">VBR</Badge>}
        </>
      }
    >
      <Stack gap="sm">
        <SimpleGrid cols={3} style={{ gridTemplateColumns: '1fr 1fr 1fr' }}>
          <Stack gap="xs">
            <Text size="xs" c="dimmed">
              Average bitrate
            </Text>
            <Text fw={500}>{data.averageBitrate}</Text>
          </Stack>
          <Stack gap="xs">
            <Text size="xs" c="dimmed">
              Min allowed bitrate
            </Text>
            <Text fw={500}>{data.minAllowedBitrate}</Text>
          </Stack>
          <Stack gap="xs">
            <Text size="xs" c="dimmed">
              Max allowed bitrate
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
    </SectionCard>
  );
}

export default BitrateGraph;
