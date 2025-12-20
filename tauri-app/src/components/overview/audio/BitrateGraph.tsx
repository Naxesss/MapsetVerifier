import {Text, Badge, Group, Paper, useMantineTheme, Stack, Box, Tooltip} from '@mantine/core';
import {LineChart} from '@mantine/charts';
import { useMemo } from 'react';
import { BitrateAnalysisResult } from '../../Types';
import { IconAlertTriangle, IconInfoCircle } from '@tabler/icons-react';

interface BitrateGraphProps {
  data: BitrateAnalysisResult;
  durationMs: number;
}

// Format time as mm:ss or h:mm:ss for longer durations
const formatTime = (seconds: number): string => {
  const hours = Math.floor(seconds / 3600);
  const mins = Math.floor((seconds % 3600) / 60);
  const secs = Math.floor(seconds % 60);

  if (hours > 0) {
    return `${hours}:${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  }
  return `${mins}:${secs.toString().padStart(2, '0')}`;
};

function BitrateGraph({ data, durationMs }: BitrateGraphProps) {
  const theme = useMantineTheme();

  // Transform data for Mantine LineChart - sample data for performance
  const chartData = useMemo(() => {
    if (!data.bitrateOverTime?.length) return [];
    const rawData = data.bitrateOverTime;
    // Sample data if too many points for performance
    const maxPoints = 200;
    const step = Math.max(1, Math.floor(rawData.length / maxPoints));
    const sampled = rawData.filter((_, i) => i % step === 0);
    return sampled.map(point => ({
      time: formatTime(point.timeMs / 1000),
      bitrate: Math.round(point.bitrate),
    }));
  }, [data.bitrateOverTime]);

  const maxBitrate = useMemo(() => {
    if (!data.bitrateOverTime?.length) return Math.max(data.maxAllowedBitrate + 50, 320);
    const maxFromData = Math.max(...data.bitrateOverTime.map(d => d.bitrate));
    return Math.max(data.maxAllowedBitrate + 50, maxFromData, 320);
  }, [data.bitrateOverTime, data.maxAllowedBitrate]);

  // Check for violations in the bitrate data
  const violations = useMemo(() => {
    if (!data.bitrateOverTime?.length) return { hasViolations: false, aboveMax: 0, belowMin: 0 };
    let aboveMax = 0;
    let belowMin = 0;
    data.bitrateOverTime.forEach(point => {
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

  // Determine the line color based on compliance
  const getLineColor = () => {
    if (data.isCompliant) return 'green.5';
    if (violations.aboveMax > 0 && violations.belowMin > 0) return 'orange.5';
    if (violations.aboveMax > 0) return 'red.5';
    if (violations.belowMin > 0) return 'yellow.5';
    return 'blue.5';
  };

  const getAdaptiveInterval = (duration: number): number => {
    if (duration <= 30) return 5;        // 0-30s: every 5s
    if (duration <= 60) return 10;       // 30-60s: every 10s
    if (duration <= 120) return 15;      // 1-2min: every 15s
    if (duration <= 300) return 30;      // 2-5min: every 30s
    if (duration <= 600) return 60;      // 5-10min: every 1min
    if (duration <= 1800) return 120;    // 10-30min: every 2min
    return 300;                          // 30min+: every 5min
  };

  const durationSeconds = durationMs / 1000;
  const timeInterval = getAdaptiveInterval(durationSeconds);

  // Generate time labels at the calculated interval
  const timeLabels: number[] = [];
  for (let t = 0; t <= durationSeconds; t += timeInterval) {
    timeLabels.push(t);
  }
  
  console.log({ durationMs, durationSeconds, interval: timeInterval, timeLabels })

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
      <Stack gap="sm">
        <Group justify="space-between">
          <Group gap="xs">
            <Text fw={600}>Bitrate Analysis</Text>
            <Tooltip
              label={`For this file type max allowed is ${data.maxAllowedBitrate} kbps. The minimum recommended is ${data.minAllowedBitrate} kbps.`}
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
            {data.isVbr && <Badge color="blue" variant="light">VBR</Badge>}
          </Group>
        </Group>
        <Group gap="lg">
          <Text size="sm" c="dimmed">
            Average: <Text span fw={500} c={data.isCompliant ? 'green.4' : 'red.4'}>
              {data.averageBitrate.toFixed(0)} kbps
            </Text>
          </Text>
          {data.isVbr && data.minBitrate && data.maxBitrate && (
            <>
              <Text size="sm" c="dimmed">
                Min: <Text span fw={500} c={data.minBitrate < data.minAllowedBitrate ? 'red.4' : 'white'}>
                  {data.minBitrate.toFixed(0)} kbps
                </Text>
              </Text>
              <Text size="sm" c="dimmed">
                Max: <Text span fw={500} c={data.maxBitrate > data.maxAllowedBitrate ? 'red.4' : 'white'}>
                  {data.maxBitrate.toFixed(0)} kbps
                </Text>
              </Text>
            </>
          )}
        </Group>
        {chartData.length > 0 ? (
          <Box pos="relative">
            <LineChart
              h={200}
              data={chartData}
              dataKey="time"
              series={[{ name: 'bitrate', label: 'Bitrate (kbps)', color: getLineColor() }]}
              curveType="linear"
              withDots={false}
              yAxisProps={{ domain: [0, maxBitrate] }}
              xAxisProps={{ domain: [0, durationMs], interval: timeInterval - 1 }}
              valueFormatter={(value) => `${value} kbps`}
              gridAxis="xy"
            />
          </Box>
        ) : (
          <Text c="dimmed" ta="center" py="xl">No bitrate data available</Text>
        )}

        {/* Compliance Status */}
        {violations.hasViolations && (
          <Group gap="xs" p="xs" bg={theme.colors.dark[6]} style={{ borderRadius: theme.radius.sm }}>
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

