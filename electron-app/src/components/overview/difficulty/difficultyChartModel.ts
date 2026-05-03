import { getDifficultyLevelColor } from '../../common/DifficultyColor.ts';
import { formatChartTime } from '../../common/TimeAxis.tsx';
import type {
  DifficultyChartDataPoint,
  DifficultyChartSeries,
  DifficultyLevel,
  DifficultyOverviewDifficulty,
  Mode,
} from '../../../Types';

export const MODE_ORDER: Mode[] = ['Standard', 'Taiko', 'Catch', 'Mania'];

export type ChartRow = {
  time: string;
  timeMs: number;
  [seriesKey: string]: number | string | null;
};
export type ChartDisplaySeries = DifficultyChartSeries & { key: string; color: string };

export type DifficultyModeGroup = {
  mode: Mode;
  difficulties: DifficultyOverviewDifficulty[];
};

export type ChartDefinition = {
  title: string;
  durationMs: number;
  /** Time between strain peaks in ms (same as overview `msPerPeak`) */
  msPerPeak: number;
  /** Full-resolution rows (400ms peaks); downsampled for display when zoomed out */
  data: ChartRow[];
  series: ChartDisplaySeries[];
  maxValue: number;
  peakTimeSeconds: number;
  valueSuffix?: string;
};

export function buildCharts(
  difficulties: DifficultyOverviewDifficulty[],
  msPerPeak?: number
): ChartDefinition[] {
  if (!msPerPeak || difficulties.length === 0) {
    return [];
  }

  const chartSeries: ChartDefinition[] = [];
  const starRatingSeries = difficulties
    .map((difficulty) => buildStarRatingSeries(difficulty, msPerPeak))
    .filter((series) => series.points.length > 0);

  if (starRatingSeries.length > 0) {
    chartSeries.push(buildChartDefinition('Star Rating', starRatingSeries, msPerPeak, '★'));
  }

  const sliderVelocitySeries = difficulties
    .map((difficulty) => buildSliderVelocitySeries(difficulty, msPerPeak))
    .filter((series) => series.points.length > 0);

  if (sliderVelocitySeries.length > 0) {
    chartSeries.push(buildChartDefinition('Slider velocity', sliderVelocitySeries, msPerPeak, '×'));
  }

  const skillSeriesMap = new Map<string, DifficultyChartSeries[]>();

  for (const difficulty of difficulties) {
    for (const skill of difficulty.skills) {
      const series = buildSkillSeries(difficulty, skill.skillName, skill.strainPeaks, msPerPeak);
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
    chartSeries.push(buildChartDefinition(skillName, skillSeries, msPerPeak));
  }

  return chartSeries;
}

function buildStarRatingSeries(
  difficulty: DifficultyOverviewDifficulty,
  msPerPeak: number
): DifficultyChartSeries {
  const points: DifficultyChartDataPoint[] = difficulty.starRatingValues.map((value, index) => ({
    timeSeconds: (index * msPerPeak) / 1000,
    value,
  }));

  return {
    skillName: 'Star Rating',
    label: difficulty.label,
    mode: difficulty.mode,
    difficultyLevel: difficulty.difficultyLevel,
    starRating: difficulty.starRating,
    points,
  };
}

function buildSliderVelocitySeries(
  difficulty: DifficultyOverviewDifficulty,
  msPerPeak: number
): DifficultyChartSeries {
  const values = difficulty.sliderVelocityValues ?? [];
  const points: DifficultyChartDataPoint[] = values.map((value, index) => ({
    timeSeconds: (index * msPerPeak) / 1000,
    value,
  }));

  return {
    skillName: 'Slider velocity',
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
  msPerPeak: number
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

function buildChartDefinition(
  title: string,
  series: DifficultyChartSeries[],
  msPerPeak: number,
  valueSuffix?: string
): ChartDefinition {
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
    const timeMs = index * msPerPeak;
    const row: ChartRow = {
      time: formatChartTime(timeMs / 1000),
      timeMs,
    };

    for (const item of displaySeries) {
      row[item.key] = item.points[index]?.value ?? null;
    }

    return row;
  });

  return {
    title,
    durationMs: Math.max(msPerPeak, maxPointCount * msPerPeak),
    msPerPeak,
    data: rawRows,
    series: displaySeries,
    maxValue: peakPoint?.value ?? 0,
    peakTimeSeconds: peakPoint?.timeSeconds ?? 0,
    valueSuffix,
  };
}

export function normalizeMode(mode: string): Mode {
  return MODE_ORDER.includes(mode as Mode) ? (mode as Mode) : 'Standard';
}

function getGraphColor(
  series: DifficultyChartSeries,
  previousSeries: DifficultyChartSeries[]
): string {
  const baseColor = hexToRgb(getDifficultyLevelColor(series.difficultyLevel));
  const difficultyLevel = normalizeDifficultyLevel(series.difficultyLevel);
  const sameDifficultyCount = previousSeries.filter(
    (item) => normalizeDifficultyLevel(item.difficultyLevel) === difficultyLevel
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
