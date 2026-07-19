import { clampColor, hexToRgb } from '../../../utils/color.ts';
import { MODE_ORDER, normalizeMode } from '../../../utils/gameMode.ts';
import { getDifficultyLevelColor } from '../../common/DifficultyColor.ts';
import { formatChartTime } from '../../common/TimeAxis.tsx';
import type {
  DifficultyChartDataPoint,
  DifficultyChartSeries,
  DifficultyLevel,
  DifficultyOverviewDifficulty,
  DifficultySamplePoint,
  Mode,
} from '../../../Types';
import type { ChartInterpolation } from '../../charts/timeSeries/types.ts';

export type ChartRow = {
  time: string;
  timeMs: number;
  [seriesKey: string]: number | string | null;
};
export type ChartDisplaySeries = DifficultyChartSeries & {
  id: string;
  key: string;
  color: string;
  dashed?: boolean;
  visibilityId?: string;
  hideFromLegend?: boolean;
  /** Row key holding the last-known (forward-filled) value, for hover lookups on sparse series
   *  that are still rendered as a smooth line (so hovering between samples shows a value). */
  hoverKey?: string;
};

export function getDifficultySeriesId(mode: string, label: string): string {
  return `${normalizeMode(mode)}::${label}`;
}

export function getDifficultySpikeSeriesId(mode: string, label: string): string {
  return `${getDifficultySeriesId(mode, label)}::spike`;
}

export type DifficultyModeGroup = {
  mode: Mode;
  difficulties: DifficultyOverviewDifficulty[];
};

export const SAMPLE_VOLUME_CHART_TITLE = 'Sample volume';

export type ChartDefinition = {
  title: string;
  durationMs: number;
  msPerPeak: number;
  data: ChartRow[];
  series: ChartDisplaySeries[];
  maxValue: number;
  peakTimeSeconds: number;
  valueSuffix?: string;
  hideLowValuesThreshold?: number;
  interpolation?: ChartInterpolation;
  showDataPoints?: boolean;
  /** Show grid sampling resolution in the card stats (SR/strain only). */
  showResolution?: boolean;
};

function toChartPoints(samples: DifficultySamplePoint[]): DifficultyChartDataPoint[] {
  return samples.map((sample) => ({
    timeMs: sample.timeMs,
    timeSeconds: sample.timeMs / 1000,
    value: sample.value,
  }));
}

export function buildCharts(
  difficulties: DifficultyOverviewDifficulty[],
  msPerPeak?: number
): ChartDefinition[] {
  if (!msPerPeak || difficulties.length === 0) {
    return [];
  }

  const chartSeries: ChartDefinition[] = [];
  const starRatingSeries = difficulties
    .map((difficulty) => buildStarRatingSeries(difficulty))
    .filter((series) => series.points.length > 0);

  const starRatingSpikeSeries = difficulties
    .map((difficulty) => buildStarRatingSpikeSeries(difficulty))
    .filter((series) => series.points.length > 0);

  if (starRatingSeries.length > 0 || starRatingSpikeSeries.length > 0) {
    chartSeries.push(
      buildChartDefinition(
        'Star Rating',
        starRatingSeries,
        msPerPeak,
        '★',
        undefined,
        'line',
        false,
        true,
        starRatingSpikeSeries
      )
    );
  }

  const sliderVelocitySeries = difficulties
    .map((difficulty) => buildSliderVelocitySeries(difficulty))
    .filter((series) => series.points.length > 0);

  if (sliderVelocitySeries.length > 0) {
    chartSeries.push(
      buildChartDefinition(
        'Slider velocity',
        sliderVelocitySeries,
        msPerPeak,
        '×',
        undefined,
        'step',
        true,
        false
      )
    );
  }

  const volumeSeries = difficulties
    .map((difficulty) => buildVolumeSeries(difficulty))
    .filter((series) => series.points.length > 0);

  if (volumeSeries.length > 0) {
    chartSeries.push(
      buildChartDefinition(
        SAMPLE_VOLUME_CHART_TITLE,
        volumeSeries,
        msPerPeak,
        '%',
        5,
        'step',
        true,
        false
      )
    );
  }

  const skillSeriesMap = new Map<string, DifficultyChartSeries[]>();

  for (const difficulty of difficulties) {
    for (const skill of difficulty.skills) {
      const series = buildSkillSeries(difficulty, skill.skillName, skill.strainSamples);
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

function buildStarRatingSeries(difficulty: DifficultyOverviewDifficulty): DifficultyChartSeries {
  return {
    skillName: 'Star Rating',
    label: difficulty.label,
    mode: difficulty.mode,
    difficultyLevel: difficulty.difficultyLevel,
    starRating: difficulty.starRating,
    points: toChartPoints(difficulty.starRatingSamples),
  };
}

function buildStarRatingSpikeSeries(
  difficulty: DifficultyOverviewDifficulty
): DifficultyChartSeries {
  return {
    skillName: 'Star Rating (spike)',
    label: `${difficulty.label} (spike)`,
    mode: difficulty.mode,
    difficultyLevel: difficulty.difficultyLevel,
    starRating: difficulty.starRating,
    points: toChartPoints(difficulty.starRatingSpikeSamples),
  };
}

function buildSliderVelocitySeries(
  difficulty: DifficultyOverviewDifficulty
): DifficultyChartSeries {
  return {
    skillName: 'Slider velocity',
    label: difficulty.label,
    mode: difficulty.mode,
    difficultyLevel: difficulty.difficultyLevel,
    starRating: difficulty.starRating,
    points: toChartPoints(difficulty.sliderVelocitySamples),
  };
}

function buildVolumeSeries(difficulty: DifficultyOverviewDifficulty): DifficultyChartSeries {
  return {
    skillName: SAMPLE_VOLUME_CHART_TITLE,
    label: difficulty.label,
    mode: difficulty.mode,
    difficultyLevel: difficulty.difficultyLevel,
    starRating: difficulty.starRating,
    points: toChartPoints(difficulty.volumeSamples),
  };
}

function buildSkillSeries(
  difficulty: DifficultyOverviewDifficulty,
  skillName: string,
  strainSamples: DifficultySamplePoint[]
): DifficultyChartSeries {
  return {
    skillName,
    label: difficulty.label,
    mode: difficulty.mode,
    difficultyLevel: difficulty.difficultyLevel,
    starRating: difficulty.starRating,
    points: toChartPoints(strainSamples),
  };
}

function buildChartDefinition(
  title: string,
  series: DifficultyChartSeries[],
  msPerPeak: number,
  valueSuffix?: string,
  hideLowValuesThreshold?: number,
  interpolation: ChartInterpolation = 'line',
  showDataPoints = false,
  showResolution = true,
  secondarySeries: DifficultyChartSeries[] = []
): ChartDefinition {
  const displaySeries: ChartDisplaySeries[] = series.map((item, index) => {
    const id = getDifficultySeriesId(item.mode, item.label);
    return {
      ...item,
      id,
      key: id,
      color: getGraphColor(item, series.slice(0, index)),
      // Different series in the same chart can sample at different times (e.g. the fine
      // cumulative grid vs. the sparser spike windows), so a hovered timestamp may not have a
      // real point for every series. Forward-fill a hover-only value so every series still shows
      // "its last known value" instead of going blank when hovering on another series' sample.
      hoverKey: `${id}__hover`,
    };
  });

  const secondaryDisplaySeries = secondarySeries.map((item) => {
    const originalLabel = item.label.replace(/ \(spike\)$/, '');
    const baseId = getDifficultySeriesId(item.mode, originalLabel);
    const spikeId = getDifficultySpikeSeriesId(item.mode, originalLabel);
    const matchingPrimary = displaySeries.find((primary) => primary.id === baseId);
    return {
      ...item,
      id: spikeId,
      key: spikeId,
      color: matchingPrimary?.color ?? getGraphColor(item, []),
      dashed: true,
      visibilityId: baseId,
      hideFromLegend: true,
      // Samples are sparse relative to the fine cumulative grid; keep the rendered line smooth
      // between them, but expose a forward-filled hover value so hovering anywhere still shows
      // "the last computed window" instead of nothing.
      hoverKey: `${spikeId}__hover`,
    };
  });

  const allDisplaySeries = [...displaySeries, ...secondaryDisplaySeries];

  const allPoints = allDisplaySeries.flatMap((item) => item.points);
  const peakPoint = allPoints.reduce<DifficultyChartDataPoint | null>((currentMax, point) => {
    if (!currentMax || point.value > currentMax.value) {
      return point;
    }

    return currentMax;
  }, null);

  const timeMsList = [
    ...new Set(allDisplaySeries.flatMap((item) => item.points.map((point) => point.timeMs))),
  ].sort((a, b) => a - b);

  const lastValueByKey: Record<string, number | null> = {};
  for (const item of allDisplaySeries) {
    lastValueByKey[item.key] = null;
  }

  const rawRows: ChartRow[] = timeMsList.map((timeMs) => {
    const row: ChartRow = {
      time: formatChartTime(timeMs / 1000),
      timeMs,
    };

    for (const item of allDisplaySeries) {
      const point = item.points.find((p) => p.timeMs === timeMs);
      if (point) {
        lastValueByKey[item.key] = point.value;
      }
      row[item.key] =
        interpolation === 'step' ? (lastValueByKey[item.key] ?? null) : (point?.value ?? null);

      if (item.hoverKey) {
        row[item.hoverKey] = lastValueByKey[item.key] ?? null;
      }
    }

    return row;
  });

  const durationMs =
    timeMsList.length > 0 ? Math.max(msPerPeak, timeMsList[timeMsList.length - 1]) : msPerPeak;

  return {
    title,
    durationMs,
    msPerPeak,
    data: rawRows,
    series: allDisplaySeries,
    maxValue: peakPoint?.value ?? 0,
    peakTimeSeconds: peakPoint?.timeSeconds ?? 0,
    valueSuffix,
    interpolation,
    showDataPoints,
    showResolution,
    ...(hideLowValuesThreshold !== undefined ? { hideLowValuesThreshold } : {}),
  };
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

export { MODE_ORDER, normalizeMode };
