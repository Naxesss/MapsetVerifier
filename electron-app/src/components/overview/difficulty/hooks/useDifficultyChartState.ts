import { useMemo } from 'react';
import { useChartViewport } from '../../../charts/timeSeries/useChartViewport.ts';
import { useSeriesVisibility } from '../../../charts/timeSeries/useSeriesVisibility.ts';
import { getDifficultySeriesId } from '../difficultyChartModel.ts';
import type { DifficultyOverviewDifficulty } from '../../../../Types';

export function useDifficultyChartState(
  durationMs: number,
  difficulties: DifficultyOverviewDifficulty[],
  viewportResetKey: string,
  visibilityResetKey: string = viewportResetKey
) {
  // Every series (primary or strain companion) resolves its visibility through the primary id -
  // see difficultyChartModel.ts's `visibilityId` wiring - so only the primary id needs to be
  // registered here, and a difficulty's selection stays consistent across display modes.
  const seriesIds = useMemo(
    () => difficulties.map((d) => getDifficultySeriesId(d.mode, d.label)),
    [difficulties]
  );

  const viewport = useChartViewport(durationMs, viewportResetKey);
  // Deliberately a coarser key than the viewport's (e.g. just the beatmapset folder): switching
  // game modes or difficulties shouldn't forget which lines you'd hidden in the legend.
  const visibility = useSeriesVisibility(seriesIds, visibilityResetKey);

  return {
    ...viewport,
    ...visibility,
    seriesIds,
  };
}

export type DifficultyChartState = ReturnType<typeof useDifficultyChartState>;
