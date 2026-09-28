import { MODE_ORDER, normalizeMode } from '../../../utils/gameMode.ts';
import { formatChartTime } from '../../common/TimeAxis.tsx';
import type {
  DifficultyChartDataPoint,
  DifficultyChartSeries,
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

export function getDifficultyStrainSeriesId(mode: string, label: string): string {
  return `${getDifficultySeriesId(mode, label)}::strain`;
}

export type DifficultyModeGroup = {
  mode: Mode;
  difficulties: DifficultyOverviewDifficulty[];
};

/**
 * Line colours, one per difficulty from easiest to hardest. Distinct hues rather than star rating
 * colours, which gave the hardest difficulties near-identical pinks; neighbours in the spread get
 * contrasting hues. Mantine shade 4, readable on the dark chart background.
 */
const SERIES_PALETTE = [
  '#4dabf7', // blue
  '#a9e34b', // lime
  '#da77f2', // grape
  '#ffa94d', // orange
  '#3bc9db', // cyan
  '#f783ac', // pink
  '#ffd43b', // yellow
  '#9775fa', // violet
  '#38d9a9', // teal
  '#ff8787', // red
  '#748ffc', // indigo
  '#69db7c', // green
];

/** Each difficulty's line colour, the same in every chart. */
function buildSeriesColours(difficulties: DifficultyOverviewDifficulty[]): Map<string, string> {
  const bySpread = [...difficulties].sort((a, b) => a.starRating - b.starRating);
  return new Map(
    bySpread.map((difficulty, index) => [
      difficulty.label,
      SERIES_PALETTE[index % SERIES_PALETTE.length],
    ])
  );
}

export const SAMPLE_VOLUME_CHART_TITLE = 'Sample volume';
export const STAR_RATING_CHART_TITLE = 'Star rating';

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

export function buildCharts(
  difficulties: DifficultyOverviewDifficulty[],
  msPerPeak?: number,
  /** Every difficulty of the mode, when `difficulties` is only the ones picked to compare, so a
   *  difficulty keeps its colour however many others are shown. */
  colourDifficulties: DifficultyOverviewDifficulty[] = difficulties
): ChartDefinition[] {
  if (!msPerPeak || difficulties.length === 0) {
    return [];
  }

  const chartSeries: ChartDefinition[] = [];
  const colours = buildSeriesColours(colourDifficulties);
  const colourFor = (series: DifficultyChartSeries) =>
    colours.get(series.label.replace(/ \(strain\)$/, '')) ?? SERIES_PALETTE[0];
  const starRatingSeries = difficulties
    .map((difficulty) => buildStarRatingSeries(difficulty))
    .filter((series) => series.points.length > 0);

  const starRatingStrainSeries = difficulties
    .map((difficulty) => buildCombinedStrainSeries(difficulty))
    .filter((series) => series.points.length > 0);

  if (starRatingSeries.length > 0 || starRatingStrainSeries.length > 0) {
    chartSeries.push(
      buildChartDefinition(
        STAR_RATING_CHART_TITLE,
        colourFor,
        starRatingSeries,
        msPerPeak,
        '★',
        undefined,
        'line',
        false,
        true,
        starRatingStrainSeries,
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
        colourFor,
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
        colourFor,
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
    chartSeries.push(buildChartDefinition(skillName, colourFor, skillSeries, msPerPeak));
  }

  return chartSeries;
}

function buildStarRatingSeries(difficulty: DifficultyOverviewDifficulty): DifficultyChartSeries {
  return {
    skillName: STAR_RATING_CHART_TITLE,
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
 * so genuine local difficulty spikes stand out. Plotted on the chart's secondary axis since it
 * isn't in Star Rating units.
 */
function buildCombinedStrainSeries(
  difficulty: DifficultyOverviewDifficulty
): DifficultyChartSeries {
  const skills = difficulty.skills;

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
    skillName: 'Combined strain',
    label: `${difficulty.label} (strain)`,
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
  colourFor: (series: DifficultyChartSeries) => string,
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
  const displaySeries: ChartDisplaySeries[] = series.map((item) => {
    const id = getDifficultySeriesId(item.mode, item.label);
    return {
      ...item,
      id,
      key: id,
      color: colourFor(item),
      // Different series in the same chart can sample at different times, so a hovered
      // timestamp may not have a real point for every series. Forward-fill a hover-only value so
      // every series still shows "its last known value" instead of going blank when hovering on
      // another series' sample.
      hoverKey: `${id}__hover`,
    };
  });

  const secondaryDisplaySeries = secondarySeries.map((item) => {
    const originalLabel = item.label.replace(/ \(strain\)$/, '');
    const baseId = getDifficultySeriesId(item.mode, originalLabel);
    const strainId = getDifficultyStrainSeriesId(item.mode, originalLabel);
    const matchingPrimary = displaySeries.find((primary) => primary.id === baseId);
    return {
      ...item,
      id: strainId,
      key: strainId,
      color: matchingPrimary?.color ?? colourFor(item),
      dashed: true,
      visibilityId: baseId,
      hideFromLegend: true,
      hoverKey: `${strainId}__hover`,
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

export { MODE_ORDER, normalizeMode };
