import { LineChart } from '@mantine/charts';
import { Alert, Badge, Box, Flex, Group, LoadingOverlay, Paper, SimpleGrid, Stack, Text, useMantineTheme } from '@mantine/core';
import { useEffect, useMemo } from 'react';
import { formatChartTime, getAdaptiveTimeInterval } from '../../common/TimeAxis.tsx';
import { useDifficultyOverview } from './hooks/useDifficultyOverview.ts';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import {
  type DifficultyChartDataPoint,
  type DifficultyChartSeries,
  type DifficultyLevel,
  type DifficultyOverviewDifficulty,
  type DifficultyOverviewResult,
  type Mode,
} from '../../../Types';
import { getDifficultyLevelColor } from '../../common/DifficultyColor.ts';

const MAX_CHART_POINTS = 300;

type ChartRow = { time: string; [seriesKey: string]: number | string | null };
type ChartDisplaySeries = DifficultyChartSeries & { key: string; color: string };
type ChartDefinition = {
  title: string;
  durationMs: number;
  data: ChartRow[];
  series: ChartDisplaySeries[];
  maxValue: number;
  peakTimeSeconds: number;
  valueSuffix?: string;
};

interface DifficultyOverviewProps {
  reloadFlag: number;
}

function DifficultyOverview({ reloadFlag }: DifficultyOverviewProps) {
  const theme = useMantineTheme();
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();
  const { data, isLoading, isError, error, refetch } = useDifficultyOverview({
    folder,
    songFolder: settings.songFolder,
  });

  useEffect(() => {
    refetch();
  }, [reloadFlag]);

  const charts = useMemo(() => buildCharts(data), [data]);
  const distinctSkillCount = useMemo(() => new Set(data?.difficulties.flatMap((difficulty) => difficulty.skills.map((skill) => skill.skillName)) ?? []).size, [data]);
  const [starRatingChart, ...skillCharts] = charts;

  if (!settings.songFolder) {
    return (
      <Alert color="yellow" title="Song folder not set">
        <Text size="sm">Please set the song folder in settings to analyze difficulty data.</Text>
      </Alert>
    );
  }

  return (
    <Box>
      <LoadingOverlay visible={isLoading} zIndex={1000} overlayProps={{ radius: 'sm', blur: 2 }} />

      {isError && (
        <Flex p="md">
          <Alert color="red" title="Error analyzing difficulty overview">
            <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>{error?.message}</Text>
            {error?.stackTrace && (
              <Text mt="sm" size="xs" c="red.3" style={{ whiteSpace: 'pre-wrap' }}>{error.stackTrace}</Text>
            )}
          </Alert>
        </Flex>
      )}

      {data && !data.success && (
        <Flex p="md">
          <Alert color="yellow" title="Analysis failed">
            <Text size="sm">{data.errorMessage}</Text>
          </Alert>
        </Flex>
      )}

      {data && data.success && (
        <Flex gap="md" p="md" direction="column">
          <SimpleGrid cols={{ base: 1, md: 3 }} spacing="md">
            <SummaryCard label="Difficulties" value={String(data.difficulties.length)} />
            <SummaryCard label="Skill charts" value={String(charts.length)} subValue={`${distinctSkillCount} unique skills`} />
            <SummaryCard label="Peak interval" value={`${(data.msPerPeak / 1000).toFixed(1)}s`} subValue="400ms strain windows" />
          </SimpleGrid>

          <Stack gap="md">
            {charts.length > 0 ? (
              <>
                {starRatingChart && <DifficultyChartCard chart={starRatingChart} />}
                {skillCharts.length > 0 && (
                  <>
                    <Text fw={600} c={theme.colors.gray[2]}>Skill Strain Analysis</Text>
                    <SimpleGrid cols={{ base: 1, xl: 2 }} spacing="md">
                      {skillCharts.map((chart) => <DifficultyChartCard key={chart.title} chart={chart} />)}
                    </SimpleGrid>
                  </>
                )}
              </>
            ) : (
              <Paper p="md" radius="md" withBorder>
                <Text c="dimmed" ta="center">No difficulty strain data available.</Text>
              </Paper>
            )}
          </Stack>
        </Flex>
      )}
    </Box>
  );
}

function DifficultyChartCard({ chart }: { chart: ChartDefinition }) {
  const theme = useMantineTheme();
  const durationSeconds = chart.durationMs / 1000;
  const timeInterval = getAdaptiveTimeInterval(durationSeconds);

  return (
    <Paper p="md" radius="md" bg={theme.colors.dark[5]}>
      <Stack gap="sm">
        <Group justify="space-between" align="flex-start">
          <Text fw={600}>{chart.title}</Text>
          <Group gap="xs">
            <Badge color={chart.valueSuffix ? 'yellow' : 'blue'} variant="light">{chart.series.length} difficulties</Badge>
            <Badge color="gray" variant="light">{formatChartDuration(chart.durationMs)}</Badge>
          </Group>
        </Group>

        <SimpleGrid cols={4} spacing="md">
          <MetricStat label="Peak" value={`${chart.maxValue.toFixed(2)}${chart.valueSuffix ?? ''}`} />
          <MetricStat label="Peak at" value={formatSeconds(chart.peakTimeSeconds)} />
          <MetricStat label="Duration" value={formatChartDuration(chart.durationMs)} />
          <MetricStat label="Resolution" value="0.4s" />
        </SimpleGrid>

        {chart.data.length > 0 ? (
          <Box pos="relative">
            <LineChart
              h={200}
              data={chart.data}
              dataKey="time"
              series={chart.series.map((series) => ({
                name: series.key,
                label: series.label,
                color: series.color,
              }))}
              curveType="linear"
              strokeWidth={2.5}
              withDots={false}
              gridAxis="xy"
              withLegend
              xAxisProps={{ domain: [0, chart.durationMs], interval: Math.max(0, timeInterval - 1) }}
              legendProps={{ verticalAlign: 'bottom', height: chart.series.length > 4 ? 44 : 30 }}
              valueFormatter={(value) => `${Number(value).toFixed(2)}${chart.valueSuffix ?? ''}`}
            />
          </Box>
        ) : (
          <Text c="dimmed" ta="center" py="xl">No chart data available</Text>
        )}
      </Stack>
    </Paper>
  );
}

function MetricStat({ label, value }: { label: string; value: string }) {
  return (
    <Stack gap={2}>
      <Text size="xs" c="dimmed">{label}</Text>
      <Text fw={600}>{value}</Text>
    </Stack>
  );
}

function SummaryCard({ label, value, subValue }: { label: string; value: string; subValue?: string }) {
  return (
    <Paper p="md" radius="md" withBorder>
      <Stack gap={4}>
        <Text size="xs" c="dimmed" tt="uppercase">{label}</Text>
        <Text fw={700} size="lg">{value}</Text>
        {subValue && <Text size="xs" c="dimmed">{subValue}</Text>}
      </Stack>
    </Paper>
  );
}

function buildCharts(data?: DifficultyOverviewResult): ChartDefinition[] {
  if (!data?.success || data.difficulties.length === 0) {
    return [];
  }

  const chartSeries: ChartDefinition[] = [];
  const starRatingSeries = data.difficulties
    .map((difficulty) => buildStarRatingSeries(difficulty, data.msPerPeak))
    .filter((series) => series.points.length > 0);

  if (starRatingSeries.length > 0) {
    chartSeries.push(buildChartDefinition('Star Rating', starRatingSeries, data.msPerPeak, '★'));
  }

  const skillSeriesMap = new Map<string, DifficultyChartSeries[]>();

  for (const difficulty of data.difficulties) {
    for (const skill of difficulty.skills) {
      const series = buildSkillSeries(difficulty, skill.skillName, skill.strainPeaks, data.msPerPeak);
      if (series.points.length === 0) continue;

      const existing = skillSeriesMap.get(skill.skillName);
      if (existing) {
        existing.push(series);
      } else {
        skillSeriesMap.set(skill.skillName, [series]);
      }
    }
  }

  for (const [skillName, skillSeries] of skillSeriesMap.entries()) {
    chartSeries.push(buildChartDefinition(skillName, skillSeries, data.msPerPeak));
  }

  return chartSeries;
}

function buildStarRatingSeries(difficulty: DifficultyOverviewDifficulty, msPerPeak: number): DifficultyChartSeries {
  const peakCount = difficulty.skills.reduce((max, skill) => Math.max(max, skill.strainPeaks.length), 0);
  const points: DifficultyChartDataPoint[] = [];

  for (let index = 0; index < peakCount; index += 1) {
    const peaksAtIndex = difficulty.skills
      .map((skill) => skill.strainPeaks[index])
      .filter((peak): peak is number => peak !== undefined);

    points.push({
      timeSeconds: (index * msPerPeak) / 1000,
      value: convertPeaksToStarRating(difficulty.mode, peaksAtIndex),
    });
  }

  return {
    skillName: 'Star Rating',
    label: difficulty.label,
    mode: difficulty.mode,
    difficultyLevel: difficulty.difficultyLevel,
    starRating: difficulty.starRating,
    points,
  };
}

function buildSkillSeries(
  difficulty: DifficultyOverviewDifficulty,
  skillName: string,
  strainPeaks: number[],
  msPerPeak: number,
): DifficultyChartSeries {
  return {
    skillName,
    label: difficulty.label,
    mode: difficulty.mode,
    difficultyLevel: difficulty.difficultyLevel,
    starRating: difficulty.starRating,
    points: strainPeaks.map((peak, index) => ({
      timeSeconds: (index * msPerPeak) / 1000,
      value: peak,
    })),
  };
}

function buildChartDefinition(title: string, series: DifficultyChartSeries[], msPerPeak: number, valueSuffix?: string): ChartDefinition {
  const displaySeries = series.map((item, index) => ({
    ...item,
    key: `series-${index}`,
    color: getGraphColor(item, series.slice(0, index)),
  }));

  const allPoints = displaySeries.flatMap((item) => item.points);
  const peakPoint = allPoints.reduce<DifficultyChartDataPoint | null>((currentMax, point) => {
    if (!currentMax || point.value > currentMax.value) {
      return point;
    }

    return currentMax;
  }, null);

  const maxPointCount = displaySeries.reduce((max, item) => Math.max(max, item.points.length), 0);
  const rawRows: ChartRow[] = Array.from({ length: maxPointCount }, (_, index) => {
    const row: ChartRow = {
      time: formatChartTime((index * msPerPeak) / 1000),
    };

    for (const item of displaySeries) {
      row[item.key] = item.points[index]?.value ?? null;
    }

    return row;
  });

  return {
    title,
    durationMs: Math.max(msPerPeak, maxPointCount * msPerPeak),
    data: sampleChartRows(rawRows),
    series: displaySeries,
    maxValue: peakPoint?.value ?? 0,
    peakTimeSeconds: peakPoint?.timeSeconds ?? 0,
    valueSuffix,
  };
}

function formatChartDuration(durationMs: number) {
  return formatChartTime(Math.max(0, Math.round(durationMs / 1000)));
}

function formatSeconds(seconds: number) {
  return formatChartTime(Math.max(0, Math.round(seconds)));
}

function sampleChartRows(rows: ChartRow[]): ChartRow[] {
  if (rows.length <= MAX_CHART_POINTS) {
    return rows;
  }

  const step = Math.max(1, Math.floor(rows.length / MAX_CHART_POINTS));
  const sampled = rows.filter((_, index) => index % step === 0);
  const last = rows[rows.length - 1];

  if (sampled[sampled.length - 1] !== last) {
    sampled.push(last);
  }

  return sampled;
}

function convertPeaksToStarRating(mode: Mode, peaks: number[]): number {
  switch (mode) {
    case 'Standard': {
      const primary = peaks[0] ?? 0;
      const secondary = peaks[1] ?? 0;
      return peaks.reduce((sum, peak) => sum + peak, 0) + Math.abs(primary - secondary) * 2;
    }
    case 'Taiko': {
      const peak = peaks[0] ?? 0;
      return 10.43 * Math.log((peak * 1.4) / 8 + 1);
    }
    default:
      return peaks.reduce((sum, peak) => sum + peak, 0);
  }
}

function getGraphColor(series: DifficultyChartSeries, previousSeries: DifficultyChartSeries[]): string {
  const baseColor = hexToRgb(getDifficultyLevelColor(series.difficultyLevel));
  const difficultyLevel = normalizeDifficultyLevel(series.difficultyLevel);
  const sameDifficultyCount = previousSeries.filter(
    (item) => normalizeDifficultyLevel(item.difficultyLevel) === difficultyLevel,
  ).length;

  let red = baseColor.r;
  let green = baseColor.g;
  let blue = baseColor.b;

  for (let index = 0; index < sameDifficultyCount; index += 1) {
    const mult = 0.7;
    const multInverse = 1 / mult;

    red *= mult;
    green *= mult;
    blue *= mult;

    switch (difficultyLevel) {
      case 'Easy':
        green *= multInverse;
        break;
      case 'Normal':
        blue *= multInverse;
        break;
      case 'Hard':
      case 'Insane':
      case 'Expert':
        red *= multInverse;
        break;
    }
  }

  return `rgb(${clampColor(red)}, ${clampColor(green)}, ${clampColor(blue)})`;
}

function normalizeDifficultyLevel(level: DifficultyLevel): DifficultyLevel {
  return level === 'Ultra' ? 'Expert' : level;
}

function hexToRgb(hex: string) {
  const normalized = hex.replace('#', '');
  return {
    r: Number.parseInt(normalized.slice(0, 2), 16),
    g: Number.parseInt(normalized.slice(2, 4), 16),
    b: Number.parseInt(normalized.slice(4, 6), 16),
  };
}

function clampColor(value: number) {
  return Math.max(0, Math.min(255, Math.round(value)));
}

export default DifficultyOverview;
