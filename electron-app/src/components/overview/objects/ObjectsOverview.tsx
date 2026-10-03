import { useEffect, useMemo } from 'react';
import ColumnUsageOverview from './components/ColumnUsageOverview.tsx';
import ObjectPercentagesOverview from './components/ObjectPercentagesOverview.tsx';
import ObjectsTimelineComparison from './components/ObjectsTimelineComparison.tsx';
import SnappingsOverview from './components/SnappingsOverview.tsx';
import { isHitsoundViewAvailable } from './hitsoundUtils.ts';
import { useObjectsAnalysis } from './hooks/useObjectsAnalysis.ts';
import { formatPreciseTime } from './timelineUtils.ts';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { usePageHints } from '../../../context/PageHintsContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import { groupByMode } from '../../../utils/gameMode';
import { StatLine } from '../../common/StatField.tsx';
import AnalysisTab from '../AnalysisTab.tsx';
import { useDifficultyPicks } from '../useDifficultyPicks.ts';
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

    return groupByMode(data.difficulties);
  }, [data]);

  const hasHitsoundModes = useMemo(
    () => groupedDifficulties.some((group) => isHitsoundViewAvailable(group.mode)),
    [groupedDifficulties]
  );

  useEffect(() => {
    setObjectsHasHitsoundModes(hasHitsoundModes);
    return () => setObjectsHasHitsoundModes(false);
  }, [hasHitsoundModes, setObjectsHasHitsoundModes]);

  const { selectedMode, selectedGroup } = useOverviewMode(groupedDifficulties);
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
          <StatLine
            items={[
              { label: 'Difficulties', value: String(data.difficulties.length) },
              { label: 'Hit objects', value: (objectCount ?? 0).toLocaleString() },
              {
                label: `Timeline range (${formatPreciseTime(data.endTimeMs - data.startTimeMs)})`,
                value: `${formatPreciseTime(data.startTimeMs)} – ${formatPreciseTime(data.endTimeMs)}`,
              },
            ]}
          />

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
