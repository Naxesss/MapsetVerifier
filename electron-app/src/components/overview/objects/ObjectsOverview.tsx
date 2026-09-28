import { SimpleGrid } from '@mantine/core';
import { useEffect, useMemo } from 'react';
import ColumnUsageOverview from './components/ColumnUsageOverview.tsx';
import ObjectPercentagesOverview from './components/ObjectPercentagesOverview.tsx';
import ObjectsTimelineComparison from './components/ObjectsTimelineComparison.tsx';
import SnappingsOverview from './components/SnappingsOverview.tsx';
import { isHitsoundViewAvailable } from './hitsoundUtils.ts';
import { useObjectsAnalysis } from './hooks/useObjectsAnalysis.ts';
import { useObjectsOverviewModeSelection } from './hooks/useObjectsOverviewModeSelection.ts';
import { formatDuration, formatTime } from './timelineUtils.ts';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { usePageHints } from '../../../context/PageHintsContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import { type Mode, type ObjectsOverviewDifficulty } from '../../../Types';
import { MODE_ORDER, normalizeMode } from '../../../utils/gameMode';
import { StatCard } from '../../common/StatField.tsx';
import AnalysisTab from '../AnalysisTab.tsx';
import type { ObjectsModeGroup } from './types.ts';

function ObjectsOverview() {
  const { selectedFolder: folder } = useBeatmap();
  const { setObjectsHasHitsoundModes } = usePageHints();
  const { settings } = useSettings();
  const { data, isLoading, isError, error } = useObjectsAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  const groupedDifficulties = useMemo<ObjectsModeGroup[]>(() => {
    if (!data?.success) return [];

    const grouped = new Map<Mode, ObjectsOverviewDifficulty[]>();
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

  const hasHitsoundModes = useMemo(
    () => groupedDifficulties.some((group) => isHitsoundViewAvailable(group.mode)),
    [groupedDifficulties]
  );

  useEffect(() => {
    setObjectsHasHitsoundModes(hasHitsoundModes);
    return () => setObjectsHasHitsoundModes(false);
  }, [hasHitsoundModes, setObjectsHasHitsoundModes]);

  const { selectedMode, setSelectedMode, selectedGroup } =
    useObjectsOverviewModeSelection(groupedDifficulties);

  const objectCount = useMemo(
    () =>
      data?.success
        ? data.difficulties.reduce((total, difficulty) => total + difficulty.objectCount, 0)
        : null,
    [data]
  );

  return (
    <AnalysisTab
      data={data}
      isLoading={isLoading}
      isError={isError}
      error={error}
      subject="objects"
    >
      {(data) => (
        <>
          <SimpleGrid cols={{ base: 1, md: 3 }} spacing="md">
            <StatCard label="Difficulties" value={String(data.difficulties.length)} />
            <StatCard label="Hit objects" value={(objectCount ?? 0).toLocaleString()} />
            <StatCard
              label={`Timeline range (${formatDuration(data.endTimeMs - data.startTimeMs)})`}
              value={`${formatTime(data.startTimeMs)} – ${formatTime(data.endTimeMs)}`}
            />
          </SimpleGrid>

          <ObjectsTimelineComparison
            startTimeMs={data.startTimeMs}
            endTimeMs={data.endTimeMs}
            groupedDifficulties={groupedDifficulties}
            difficulties={selectedGroup?.difficulties ?? []}
            selectedMode={selectedMode ?? selectedGroup?.mode}
            onModeChange={setSelectedMode}
          />
          <SnappingsOverview
            groupedDifficulties={groupedDifficulties}
            selectedMode={selectedMode ?? selectedGroup?.mode}
            onModeChange={setSelectedMode}
            difficulties={selectedGroup?.difficulties ?? []}
          />
          <ObjectPercentagesOverview
            groupedDifficulties={groupedDifficulties}
            selectedMode={selectedMode ?? selectedGroup?.mode}
            onModeChange={setSelectedMode}
            difficulties={selectedGroup?.difficulties ?? []}
          />
          <ColumnUsageOverview
            groupedDifficulties={groupedDifficulties}
            selectedMode={selectedMode ?? selectedGroup?.mode}
            onModeChange={setSelectedMode}
            difficulties={selectedGroup?.difficulties ?? []}
          />
        </>
      )}
    </AnalysisTab>
  );
}

export default ObjectsOverview;
