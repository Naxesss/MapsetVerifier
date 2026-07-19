import { useMemo } from 'react';
import { useChartViewport } from '../../../charts/timeSeries/useChartViewport.ts';
import { useSeriesVisibility } from '../../../charts/timeSeries/useSeriesVisibility.ts';
import { getDifficultySeriesId, getDifficultySpikeSeriesId } from '../difficultyChartModel.ts';
import type { DifficultyOverviewDifficulty } from '../../../../Types';

export function useDifficultyChartState(
  durationMs: number,
  difficulties: DifficultyOverviewDifficulty[],
  resetKey: string
) {
  // Registers both the primary and spike ids for every difficulty, so a chart can toggle
  // visibility by either id depending on which one it currently renders (e.g. the Star Rating
  // chart's "spikes only" display mode toggles by the spike id instead of the primary one).
  const seriesIds = useMemo(
    () =>
      difficulties.flatMap((d) => [
        getDifficultySeriesId(d.mode, d.label),
        getDifficultySpikeSeriesId(d.mode, d.label),
      ]),
    [difficulties]
  );

  const viewport = useChartViewport(durationMs, resetKey);
  const visibility = useSeriesVisibility(seriesIds, resetKey);

  return {
    ...viewport,
    ...visibility,
    seriesIds,
  };
}

export type DifficultyChartState = ReturnType<typeof useDifficultyChartState>;
