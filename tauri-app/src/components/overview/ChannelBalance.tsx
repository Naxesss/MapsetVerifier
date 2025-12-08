import { Text, Badge, Group, Paper, useMantineTheme, Progress, Stack } from '@mantine/core';
import { AreaChart } from '@mantine/charts';
import { useMemo } from 'react';
import { ChannelAnalysisResult } from '../../Types';

interface ChannelBalanceProps {
  data: ChannelAnalysisResult;
}

function getSeverityColor(severity: string): string {
  switch (severity) {
    case 'None': return 'green';
    case 'Minor': return 'yellow';
    case 'Warning': return 'orange';
    case 'Severe': return 'red';
    default: return 'gray';
  }
}

function ChannelBalance({ data }: ChannelBalanceProps) {
  const theme = useMantineTheme();

  // Transform data for Mantine AreaChart - sample data for performance
  const chartData = useMemo(() => {
    if (!data.balanceOverTime?.length) return [];
    const rawData = data.balanceOverTime;
    // Sample data if too many points for performance
    const maxPoints = 200;
    const step = Math.max(1, Math.floor(rawData.length / maxPoints));
    const sampled = rawData.filter((_, i) => i % step === 0);
    return sampled.map(point => ({
      time: `${(point.timeMs / 1000).toFixed(1)}s`,
      left: Math.round(point.leftLevel * 100),
      right: Math.round(point.rightLevel * 100),
    }));
  }, [data.balanceOverTime]);

  const leftPercent = Math.round(data.leftChannelLevel * 100);
  const rightPercent = Math.round(data.rightChannelLevel * 100);

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[7]}>
      <Group justify="space-between" mb="sm">
        <Text fw={600}>Channel Balance</Text>
        <Group gap="xs">
          <Badge color={getSeverityColor(data.severity)} variant="light">
            {data.severity === 'None' ? 'Balanced' : data.severity}
          </Badge>
          <Badge color="gray" variant="light">{data.isMono ? 'Mono' : 'Stereo'}</Badge>
        </Group>
      </Group>
      <Stack gap="xs" mb="md">
        <Group gap="xs">
          <Text size="sm" w={60} c="blue.4">Left</Text>
          <Progress value={leftPercent} color="blue" style={{ flex: 1 }} size="lg" />
          <Text size="sm" w={40}>{leftPercent}%</Text>
        </Group>
        <Group gap="xs">
          <Text size="sm" w={60} c="pink.4">Right</Text>
          <Progress value={rightPercent} color="pink" style={{ flex: 1 }} size="lg" />
          <Text size="sm" w={40}>{rightPercent}%</Text>
        </Group>
      </Stack>
      <Group gap="lg" mb="md">
        <Text size="sm" c="dimmed">Stereo Width: <Text span fw={500} c="white">{(data.stereoWidth * 100).toFixed(0)}%</Text></Text>
        <Text size="sm" c="dimmed">Phase: <Text span fw={500} c="white">{data.phaseCorrelation.toFixed(2)}</Text></Text>
        {data.louderChannel !== 'Balanced' && (
          <Text size="sm" c="dimmed">Louder: <Text span fw={500} c="white">{data.louderChannel}</Text></Text>
        )}
      </Group>
      {chartData.length > 0 ? (
        <AreaChart
          h={180}
          data={chartData}
          dataKey="time"
          series={[
            { name: 'left', label: 'Left Channel', color: 'blue.6' },
            { name: 'right', label: 'Right Channel', color: 'pink.6' },
          ]}
          curveType="monotone"
          withDots={false}
          yAxisProps={{ domain: [0, 100] }}
          xAxisProps={{ tickMargin: 10 }}
          valueFormatter={(value) => `${value}%`}
          fillOpacity={0.5}
          gridAxis="xy"
          withLegend
          legendProps={{ verticalAlign: 'bottom', height: 30 }}
        />
      ) : (
        <Text c="dimmed" ta="center" py="xl">No channel balance data available</Text>
      )}
    </Paper>
  );
}

export default ChannelBalance;

