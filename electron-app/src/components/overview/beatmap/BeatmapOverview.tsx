import { useMemo } from 'react';
import DifficultySettingsInfo from './DifficultySettingsInfo';
import GeneralSettingsInfo from './GeneralSettingsInfo';
import { useBeatmapAnalysis } from './hooks/useBeatmapAnalysis';
import StatisticsInfo from './StatisticsInfo';
import { useBeatmap } from '../../../context/BeatmapContext';
import { useSettings } from '../../../context/SettingsContext';
import { MODE_ORDER, normalizeMode } from '../../../utils/gameMode';
import GameModeSelector from '../../common/GameModeSelector.tsx';
import AnalysisTab from '../AnalysisTab.tsx';
import DifficultyPicks, { useDifficultyPicks } from '../DifficultyPicks.tsx';
import { useOverviewMode } from '../useOverviewMode.ts';

function BeatmapOverview() {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();

  const { data, isLoading, isError, error } = useBeatmapAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  // Settings only compare within a mode, so a hybrid mapset shows one mode at a time.
  const groupedDifficulties = useMemo(() => {
    const statistics = data?.success ? data.statistics : [];
    return MODE_ORDER.map((mode) => ({
      mode,
      difficulties: statistics.filter((entry) => normalizeMode(entry.mode) === mode),
    })).filter((group) => group.difficulties.length > 0);
  }, [data]);

  const { selectedMode, setSelectedMode, selectedGroup } = useOverviewMode(groupedDifficulties);
  const modeDifficulties = useMemo(() => selectedGroup?.difficulties ?? [], [selectedGroup]);
  const starRatings = useMemo(
    () => new Map(modeDifficulties.map((entry) => [entry.version, entry.starRating ?? 0])),
    [modeDifficulties]
  );

  const { isShown: isPicked } = useDifficultyPicks(modeDifficulties);
  const isShown = (item: { mode: string; version: string }) =>
    (!selectedMode || normalizeMode(item.mode) === selectedMode) && isPicked(item.version);

  return (
    <AnalysisTab
      data={data}
      isLoading={isLoading}
      isError={isError}
      error={error}
      subject="beatmap"
    >
      {(data) => (
        <>
          <GameModeSelector
            groupedDifficulties={groupedDifficulties}
            selectedMode={selectedMode}
            onModeChange={setSelectedMode}
          />
          <DifficultyPicks difficulties={modeDifficulties} />
          <StatisticsInfo statistics={data.statistics.filter(isShown)} starRatings={starRatings} />
          <GeneralSettingsInfo
            generalSettings={data.generalSettings.filter(isShown)}
            starRatings={starRatings}
          />
          <DifficultySettingsInfo
            difficultySettings={data.difficultySettings.filter(isShown)}
            starRatings={starRatings}
          />
        </>
      )}
    </AnalysisTab>
  );
}

export default BeatmapOverview;
