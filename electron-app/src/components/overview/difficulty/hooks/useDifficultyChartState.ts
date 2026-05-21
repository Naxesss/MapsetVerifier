import { useMemo } from 'react';
import { useChartViewport } from '../../../charts/timeSeries/useChartViewport.ts';
import { useSeriesVisibility } from '../../../charts/timeSeries/useSeriesVisibility.ts';
import { getDifficultySeriesId } from '../difficultyChartModel.ts';
import type { DifficultyOverviewDifficulty } from '../../../../Types';

export function useDifficultyChartState(
  durationMs: number,
  difficulties: DifficultyOverviewDifficulty[],
  resetKey: string
) {
  const seriesIds = useMemo(
    () => difficulties.map((d) => getDifficultySeriesId(d.mode, d.label)),
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
