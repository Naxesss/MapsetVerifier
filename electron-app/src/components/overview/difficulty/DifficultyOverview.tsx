import { Grid, Stack } from '@mantine/core';
import { IconChartLine } from '@tabler/icons-react';
import { useMemo } from 'react';
import { DifficultyChartCard } from './DifficultyChartCard.tsx';
import {
  buildCharts,
  MODE_ORDER,
  normalizeMode,
  SAMPLE_VOLUME_CHART_TITLE,
  STAR_RATING_CHART_TITLE,
  type DifficultyModeGroup,
} from './difficultyChartModel.ts';
import { DifficultySpreadSummary } from './DifficultySummaryCards.tsx';
import AnalysisTab from '../AnalysisTab.tsx';
import DifficultyPicks, { useDifficultyPicks } from '../DifficultyPicks.tsx';
import { useDifficultyChartState } from './hooks/useDifficultyChartState.ts';
import { useDifficultyOverview } from './hooks/useDifficultyOverview.ts';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import EmptyState from '../../common/EmptyState.tsx';
import GameModeSelector from '../../common/GameModeSelector.tsx';
import { SectionTitle } from '../../common/Headings.tsx';
import { useOverviewMode } from '../useOverviewMode.ts';
import type { DifficultyOverviewDifficulty, Mode } from '../../../Types';

const EMPTY_DIFFICULTIES: DifficultyOverviewDifficulty[] = [];

function DifficultyOverview() {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();
  const { data, isLoading, isError, error } = useDifficultyOverview({
    folder,
    songFolder: settings.songFolder,
  });

  const groupedDifficulties = useMemo<DifficultyModeGroup[]>(() => {
    if (!data?.success) {
      return [];
    }

    const grouped = new Map<Mode, DifficultyOverviewDifficulty[]>();

    for (const difficulty of data.difficulties) {
      const mode = normalizeMode(difficulty.mode);
      const modeDifficulties = grouped.get(mode);

      if (modeDifficulties) {
        modeDifficulties.push(difficulty);
      } else {
        grouped.set(mode, [difficulty]);
      }
    }

    return MODE_ORDER.filter((mode) => grouped.has(mode)).map((mode) => ({
      mode,
      difficulties: grouped.get(mode) ?? [],
    }));
  }, [data]);

  const { setSelectedMode, selectedGroup } = useOverviewMode(groupedDifficulties);
  const modeDifficulties = selectedGroup?.difficulties ?? EMPTY_DIFFICULTIES;
  // The graphs show the difficulties picked to compare; the spread summary stays about all of them.
  const pickable = useMemo(
    () => modeDifficulties.map((d) => ({ version: d.label, starRating: d.starRating })),
    [modeDifficulties]
  );
  const { isShown } = useDifficultyPicks(pickable);
  const shownKey = modeDifficulties.map((d) => (isShown(d.label) ? '1' : '0')).join('');
  const selectedDifficulties = useMemo(
    () => modeDifficulties.filter((_, i) => shownKey[i] === '1'),
    [modeDifficulties, shownKey]
  );
  const charts = useMemo(
    () => buildCharts(selectedDifficulties, data?.msPerPeak, modeDifficulties),
    [data?.msPerPeak, selectedDifficulties, modeDifficulties]
  );

  const durationMs = charts[0]?.durationMs ?? data?.msPerPeak ?? 0;
  const chartResetKey = useMemo(
    () =>
      `${folder ?? ''}:${selectedGroup?.mode ?? ''}:${selectedDifficulties.map((d) => d.label).join('|')}`,
    [folder, selectedDifficulties, selectedGroup?.mode]
  );

  const chartState = useDifficultyChartState(
    durationMs,
    selectedDifficulties,
    chartResetKey,
    folder ?? ''
  );

  const starRatingChart = charts.find((c) => c.title === STAR_RATING_CHART_TITLE);
  const sliderVelocityChart = charts.find((c) => c.title === 'Slider velocity');
  const sampleVolumeChart = charts.find((c) => c.title === SAMPLE_VOLUME_CHART_TITLE);
  const skillCharts = charts.filter(
    (c) =>
      c.title !== STAR_RATING_CHART_TITLE &&
      c.title !== 'Slider velocity' &&
      c.title !== SAMPLE_VOLUME_CHART_TITLE
  );

  return (
    <AnalysisTab
      data={data}
      isLoading={isLoading}
      isError={isError}
      error={error}
      subject="difficulties"
    >
      {() => (
        <>
          <GameModeSelector
            groupedDifficulties={groupedDifficulties}
            selectedMode={selectedGroup?.mode}
            onModeChange={setSelectedMode}
          />

          <DifficultySpreadSummary difficulties={modeDifficulties} />

          <DifficultyPicks difficulties={pickable} />

          <Stack gap="md">
            {charts.length > 0 ? (
              <>
                {starRatingChart && (
                  <DifficultyChartCard chart={starRatingChart} chartState={chartState} />
                )}
                {sliderVelocityChart && (
                  <DifficultyChartCard chart={sliderVelocityChart} chartState={chartState} />
                )}
                {sampleVolumeChart && (
                  <DifficultyChartCard chart={sampleVolumeChart} chartState={chartState} />
                )}
                {skillCharts.length > 0 && (
                  <>
                    <SectionTitle>Skill strain</SectionTitle>
                    <Grid grow gutter="md">
                      {skillCharts.map((chart) => (
                        <Grid.Col key={chart.title} span={{ base: 12, lg: 6, xl: 4 }}>
                          <DifficultyChartCard chart={chart} chartState={chartState} />
                        </Grid.Col>
                      ))}
                    </Grid>
                  </>
                )}
              </>
            ) : (
              <EmptyState icon={IconChartLine} title="No difficulty strain data available" />
            )}
          </Stack>
        </>
      )}
    </AnalysisTab>
  );
}

export default DifficultyOverview;
