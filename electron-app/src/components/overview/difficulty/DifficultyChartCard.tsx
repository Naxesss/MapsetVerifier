import { Box, Group, Paper, SimpleGrid, Stack, Switch, Text, useMantineTheme } from '@mantine/core';
import { useMemo, useState } from 'react';
import { FilterableLineChart } from '../../common/FilterableLineChart.tsx';
import { formatChartTime } from '../../common/TimeAxis.tsx';
import type { ChartDefinition, ChartRow } from './difficultyChartModel.ts';

function formatChartDuration(durationMs: number) {
  return formatChartTime(Math.max(0, Math.round(durationMs / 1000)));
}

function formatSeconds(seconds: number) {
  return formatChartTime(Math.max(0, Math.round(seconds)));
}

function formatStrainResolution(msPerPeak: number): string {
  const s = msPerPeak / 1000;
  const trimmed = s.toFixed(3).replace(/\.?0+$/, '');
  return `${trimmed}s`;
}

function MetricStat({ label, value }: { label: string; value: string }) {
  return (
    <Stack gap={2}>
      <Text size="xs" c="dimmed">
        {label}
      </Text>
      <Text fw={600}>{value}</Text>
    </Stack>
  );
}

function computePeakFromRows(
  rows: ChartRow[],
  seriesKeys: string[]
): { maxValue: number; peakTimeSeconds: number } {
  let best: { value: number; timeSeconds: number } | null = null;

  for (const row of rows) {
    const timeMs = row.timeMs;
    if (typeof timeMs !== 'number') continue;

    const timeSeconds = timeMs / 1000;

    for (const key of seriesKeys) {
      const v = row[key];
      if (typeof v !== 'number') continue;

      if (!best || v > best.value) {
        best = { value: v, timeSeconds };
      }
    }
  }

  return {
    maxValue: best?.value ?? 0,
    peakTimeSeconds: best?.timeSeconds ?? 0,
  };
}

function formatChartMetricValue(value: number, valueSuffix: string | undefined): string {
  if (valueSuffix === '%') {
    return `${Math.round(value)}${valueSuffix}`;
  }
  return `${value.toFixed(2)}${valueSuffix ?? ''}`;
}

export function DifficultyChartCard({ chart }: { chart: ChartDefinition }) {
  const theme = useMantineTheme();
  const [ignoreLowVolume, setIgnoreLowVolume] = useState(true);

  const displayData = useMemo(() => {
    const threshold = chart.hideLowValuesThreshold;
    if (threshold === undefined || !ignoreLowVolume) {
      return chart.data;
    }

    const keys = chart.series.map((item) => item.key);
    const carry: Record<string, number | null> = {};
    for (const key of keys) {
      carry[key] = null;
    }

    return chart.data.map((row) => {
      const next: ChartRow = { ...row };
      for (const key of keys) {
        const v = next[key];
        if (typeof v !== 'number') continue;

        if (v > threshold) {
          carry[key] = v;
        } else if (carry[key] !== null) {
          next[key] = carry[key];
        } else {
          next[key] = null;
        }
      }
      return next;
    });
  }, [chart.data, chart.hideLowValuesThreshold, chart.series, ignoreLowVolume]);

  const { peakValue, peakAtSeconds } = useMemo(() => {
    const threshold = chart.hideLowValuesThreshold;
    if (threshold === undefined || !ignoreLowVolume) {
      return { peakValue: chart.maxValue, peakAtSeconds: chart.peakTimeSeconds };
    }

    const keys = chart.series.map((item) => item.key);
    const { maxValue, peakTimeSeconds } = computePeakFromRows(displayData, keys);
    return { peakValue: maxValue, peakAtSeconds: peakTimeSeconds };
  }, [
    chart.hideLowValuesThreshold,
    chart.maxValue,
    chart.peakTimeSeconds,
    chart.series,
    displayData,
    ignoreLowVolume,
  ]);

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[5]} style={{ overflow: 'visible' }}>
      <Stack gap="sm">
        <Text fw={600}>{chart.title}</Text>

        <SimpleGrid cols={4} spacing="md">
          <MetricStat
            label="Peak"
            value={formatChartMetricValue(peakValue, chart.valueSuffix)}
          />
          <MetricStat label="Peak at" value={formatSeconds(peakAtSeconds)} />
          <MetricStat label="Duration" value={formatChartDuration(chart.durationMs)} />
          <MetricStat label="Resolution" value={formatStrainResolution(chart.msPerPeak)} />
        </SimpleGrid>

        {chart.hideLowValuesThreshold !== undefined && (
          <Group justify="flex-end">
            <Switch
              size="sm"
              checked={ignoreLowVolume}
              onChange={(e) => setIgnoreLowVolume(e.currentTarget.checked)}
              label={`Ignore ≤${chart.hideLowValuesThreshold}% volume`}
              styles={{
                track: {
                  backgroundColor: ignoreLowVolume ? undefined : theme.colors.dark[8],
                },
              }}
            />
          </Group>
        )}

        {chart.data.length > 0 ? (
          <Box pos="relative" style={{ overflow: 'visible' }}>
            <FilterableLineChart
              h={200}
              data={displayData}
              dataKey="timeMs"
              durationMs={chart.durationMs}
              series={chart.series.map((series) => ({
                name: series.key,
                label: series.label,
                color: series.color,
              }))}
              curveType="linear"
              strokeWidth={2.5}
              withDots={false}
              gridAxis="xy"
              xAxisProps={{ interval: 'preserveStartEnd' }}
              legendProps={{
                verticalAlign: 'bottom',
                wrapperStyle: {
                  overflow: 'visible',
                  width: '100%',
                  maxWidth: '100%',
                },
              }}
              styles={{
                legend: {
                  height: 'auto',
                  minHeight: 'unset',
                  alignItems: 'flex-start',
                  justifyContent: 'flex-end',
                },
                legendItem: {
                  alignItems: 'flex-start',
                  maxWidth: '100%',
                },
                legendItemName: {
                  whiteSpace: 'normal',
                  wordBreak: 'break-word',
                  lineHeight: 1.35,
                },
              }}
              valueFormatter={(value) =>
                formatChartMetricValue(Number(value), chart.valueSuffix)
              }
            />
          </Box>
        ) : (
          <Text c="dimmed" ta="center" py="xl">
            No chart data available
          </Text>
        )}
      </Stack>
    </Paper>
  );
}
