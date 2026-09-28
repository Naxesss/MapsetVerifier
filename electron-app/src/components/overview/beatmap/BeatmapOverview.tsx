import DifficultySettingsInfo from './DifficultySettingsInfo';
import GeneralSettingsInfo from './GeneralSettingsInfo';
import { useBeatmapAnalysis } from './hooks/useBeatmapAnalysis';
import StatisticsInfo from './StatisticsInfo';
import { useBeatmap } from '../../../context/BeatmapContext';
import { useSettings } from '../../../context/SettingsContext';
import AnalysisTab from '../AnalysisTab.tsx';

function BeatmapOverview() {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();

  const { data, isLoading, isError, error } = useBeatmapAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

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
          <StatisticsInfo statistics={data.statistics} />
          <GeneralSettingsInfo generalSettings={data.generalSettings} />
          <DifficultySettingsInfo difficultySettings={data.difficultySettings} />
        </>
      )}
    </AnalysisTab>
  );
}

export default BeatmapOverview;
