import { useEffect, useMemo } from 'react';
import ColumnUsageOverview from './components/ColumnUsageOverview.tsx';
import ObjectPercentagesOverview from './components/ObjectPercentagesOverview.tsx';
import ObjectsTimelineComparison from './components/ObjectsTimelineComparison.tsx';
import SnappingsOverview from './components/SnappingsOverview.tsx';
import { isHitsoundViewAvailable } from './hitsoundUtils.ts';
import { useObjectsAnalysis } from './hooks/useObjectsAnalysis.ts';
import { formatDuration, formatTime } from './timelineUtils.ts';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { usePageHints } from '../../../context/PageHintsContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import { type Mode, type ObjectsOverviewDifficulty } from '../../../Types';
import { MODE_ORDER, normalizeMode } from '../../../utils/gameMode';
import GameModeSelector from '../../common/GameModeSelector.tsx';
import { StatLine } from '../../common/StatField.tsx';
import AnalysisTab from '../AnalysisTab.tsx';
import DifficultyPicks, { useDifficultyPicks } from '../DifficultyPicks.tsx';
import { useOverviewMode } from '../useOverviewMode.ts';
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

  const { selectedMode, setSelectedMode, selectedGroup } = useOverviewMode(groupedDifficulties);
  const modeDifficulties = useMemo(() => selectedGroup?.difficulties ?? [], [selectedGroup]);
  // The timeline and the tables show the difficulties picked to compare, or all of them.
  const { isShown } = useDifficultyPicks(modeDifficulties);
  const shownDifficulties = modeDifficulties.filter((difficulty) => isShown(difficulty.version));

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
          <GameModeSelector
            groupedDifficulties={groupedDifficulties}
            selectedMode={selectedMode}
            onModeChange={setSelectedMode}
          />

          <StatLine
            items={[
              { label: 'Difficulties', value: String(data.difficulties.length) },
              { label: 'Hit objects', value: (objectCount ?? 0).toLocaleString() },
              {
                label: `Timeline range (${formatDuration(data.endTimeMs - data.startTimeMs)})`,
                value: `${formatTime(data.startTimeMs)} – ${formatTime(data.endTimeMs)}`,
              },
            ]}
          />

          <DifficultyPicks difficulties={modeDifficulties} />

          <ObjectsTimelineComparison
            startTimeMs={data.startTimeMs}
            endTimeMs={data.endTimeMs}
            groupedDifficulties={groupedDifficulties}
            difficulties={shownDifficulties}
            selectedMode={selectedMode}
          />
          <SnappingsOverview difficulties={shownDifficulties} />
          <ObjectPercentagesOverview mode={selectedMode} difficulties={shownDifficulties} />
          <ColumnUsageOverview mode={selectedMode} difficulties={shownDifficulties} />
        </>
      )}
    </AnalysisTab>
  );
}

export default ObjectsOverview;
