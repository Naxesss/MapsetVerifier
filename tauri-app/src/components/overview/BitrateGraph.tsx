import { Text, Badge, Group, Paper, useMantineTheme } from '@mantine/core';
import { LineChart } from '@mantine/charts';
import { useMemo } from 'react';
import { BitrateAnalysisResult } from '../../Types';

interface BitrateGraphProps {
  data: BitrateAnalysisResult;
}

function BitrateGraph({ data }: BitrateGraphProps) {
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
      time: `${(point.timeMs / 1000).toFixed(1)}s`,
      bitrate: Math.round(point.bitrate),
    }));
  }, [data.bitrateOverTime]);

  const maxBitrate = useMemo(() => {
    if (!data.bitrateOverTime?.length) return data.maxAllowedBitrate + 50;
    const maxFromData = Math.max(...data.bitrateOverTime.map(d => d.bitrate));
    return Math.max(data.maxAllowedBitrate + 50, maxFromData);
  }, [data.bitrateOverTime, data.maxAllowedBitrate]);

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[7]}>
      <Group justify="space-between" mb="sm">
        <Text fw={600}>Bitrate Analysis</Text>
        <Group gap="xs">
          <Badge color={data.isCompliant ? 'green' : 'red'} variant="light">
            {data.isCompliant ? 'Compliant' : 'Non-Compliant'}
          </Badge>
          {data.isVbr && <Badge color="blue" variant="light">VBR</Badge>}
        </Group>
      </Group>
      <Group gap="lg" mb="md">
        <Text size="sm" c="dimmed">Average: <Text span fw={500} c="white">{data.averageBitrate.toFixed(0)} kbps</Text></Text>
        {data.isVbr && data.minBitrate && data.maxBitrate && (
          <>
            <Text size="sm" c="dimmed">Min: <Text span fw={500} c="white">{data.minBitrate.toFixed(0)} kbps</Text></Text>
            <Text size="sm" c="dimmed">Max: <Text span fw={500} c="white">{data.maxBitrate.toFixed(0)} kbps</Text></Text>
          </>
        )}
        <Text size="sm" c="dimmed">Allowed: <Text span fw={500} c="white">{data.minAllowedBitrate}-{data.maxAllowedBitrate} kbps</Text></Text>
      </Group>
      {chartData.length > 0 ? (
        <LineChart
          h={200}
          data={chartData}
          dataKey="time"
          series={[{ name: 'bitrate', label: 'Bitrate', color: data.isCompliant ? 'green.5' : 'red.5' }]}
          curveType="monotone"
          withDots={false}
          yAxisProps={{ domain: [0, maxBitrate] }}
          xAxisProps={{ tickMargin: 10 }}
          valueFormatter={(value) => `${value} kbps`}
          referenceLines={[
            { y: data.maxAllowedBitrate, label: 'Max', color: 'red.6' },
            { y: data.minAllowedBitrate, label: 'Min', color: 'yellow.6' },
          ]}
          gridAxis="xy"
        />
      ) : (
        <Text c="dimmed" ta="center" py="xl">No bitrate data available</Text>
      )}
      <Text size="xs" c="dimmed" mt="xs">{data.complianceMessage}</Text>
    </Paper>
  );
}

export default BitrateGraph;

