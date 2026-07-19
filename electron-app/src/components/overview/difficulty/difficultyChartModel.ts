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
  /** Plots against the chart's secondary (right) axis instead of sharing the primary one -
   *  for series on a fundamentally different scale/unit than the chart's main series. */
  useSecondaryAxis?: boolean;
  /** Overrides the chart-wide value suffix for this series only (e.g. '' for a secondary-axis
   *  series in different units, so it doesn't inherit the primary series' unit label). */
  valueSuffix?: string;
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
  /** 'zeroBased' (default) always anchors the axis at 0. 'fitToData' zooms to the visible value
   *  range instead, so charts whose values stay within a narrow band near the top of the scale
   *  (e.g. Star Rating, which rarely approaches 0) show relative differences more clearly. */
  yAxisMode?: 'zeroBased' | 'fitToData';
  /** Suffix for the secondary (right) axis, when the chart has series plotted against it. */
  secondaryValueSuffix?: string;
};

function toChartPoints(samples: DifficultySamplePoint[]): DifficultyChartDataPoint[] {
  return samples.map((sample) => ({
    timeMs: sample.timeMs,
    timeSeconds: sample.timeMs / 1000,
    value: sample.value,
  }));
}

export type BuildChartsOptions = {
  /** Excludes osu!standard's Aim skill(s) from the combined strain spike line. */
  excludeAimFromCombinedStrain?: boolean;
};

export function buildCharts(
  difficulties: DifficultyOverviewDifficulty[],
  msPerPeak?: number,
  options: BuildChartsOptions = {}
): ChartDefinition[] {
  if (!msPerPeak || difficulties.length === 0) {
    return [];
  }

  const chartSeries: ChartDefinition[] = [];
  const starRatingSeries = difficulties
    .map((difficulty) => buildStarRatingSeries(difficulty))
    .filter((series) => series.points.length > 0);

  const starRatingSpikeSeries = difficulties
    .map((difficulty) => buildCombinedStrainSpikeSeries(difficulty, options))
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
        starRatingSpikeSeries,
        'fitToData',
        '%' // Each skill is normalized to a % of its own peak before combining, see below.
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

/**
 * Combines every skill's strain at each sample time into a single "how hard is it right now"
 * line. Skills aren't on comparable scales (e.g. osu!std's Speed can run far higher than its
 * Aim), so each skill is first normalized to a percentage of its own peak for this map. They're
 * then combined as a weighted average by each skill's own `difficultyValue` - its real aggregate
 * contribution to this map's difficulty - so a skill that barely factors into the actual Star
 * Rating (e.g. Aim on a speed-focused map) doesn't get an outsized say just for existing, while a
 * skill that dominates the real rating dominates this line too. Unlike Star Rating - a
 * deliberately compressed, diminishing-returns scale - this isn't bounded/smoothed the same way,
 * so genuine local spikes stand out. Plotted on the chart's secondary axis since it isn't in Star
 * Rating units.
 */
function buildCombinedStrainSpikeSeries(
  difficulty: DifficultyOverviewDifficulty,
  options: BuildChartsOptions
): DifficultyChartSeries {
  const skills = options.excludeAimFromCombinedStrain
    ? difficulty.skills.filter((skill) => !skill.skillName.startsWith('Aim'))
    : difficulty.skills;

  const bySkillTimeMs = skills[0]?.strainSamples.map((sample) => sample.timeMs) ?? [];

  const skillPeaks = skills.map((skill) =>
    skill.strainSamples.reduce((max, sample) => Math.max(max, sample.value), 0)
  );

  const totalWeight = skills.reduce((sum, skill) => sum + skill.difficultyValue, 0);
  // If every skill's difficultyValue is 0 (unexpected, but possible for a near-empty map), fall
  // back to equal weighting instead of dividing by zero.
  const weights =
    totalWeight > 0
      ? skills.map((skill) => skill.difficultyValue / totalWeight)
      : skills.map(() => 1 / Math.max(skills.length, 1));

  const points: DifficultyChartDataPoint[] = bySkillTimeMs.map((timeMs, index) => {
    const value = skills.reduce((sum, skill, skillIndex) => {
      const peak = skillPeaks[skillIndex];
      const raw = skill.strainSamples[index]?.value ?? 0;
      const normalized = peak > 0 ? (raw / peak) * 100 : 0;
      return sum + normalized * weights[skillIndex];
    }, 0);

    return { timeMs, timeSeconds: timeMs / 1000, value };
  });

  return {
    skillName: 'Combined strain (spike)',
    label: `${difficulty.label} (spike)`,
    mode: difficulty.mode,
    difficultyLevel: difficulty.difficultyLevel,
    starRating: difficulty.starRating,
    points,
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
  secondarySeries: DifficultyChartSeries[] = [],
  yAxisMode: 'zeroBased' | 'fitToData' = 'zeroBased',
  secondaryValueSuffix?: string
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
      hoverKey: `${spikeId}__hover`,
      // A different metric on a different scale than the primary series (e.g. raw strain
      // alongside Star Rating) - its own axis and unit label, not the primary's.
      useSecondaryAxis: true,
      valueSuffix: secondaryValueSuffix ?? '',
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
    yAxisMode,
    secondaryValueSuffix,
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
