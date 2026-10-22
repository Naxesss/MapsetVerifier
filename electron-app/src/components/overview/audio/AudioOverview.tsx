import { SimpleGrid } from '@mantine/core';
import ChannelBalance from './ChannelBalance';
import DynamicRange from './DynamicRange';
import FormatInfo from './FormatInfo';
import FrequencyAnalysis from './FrequencyAnalysis';
import { useAudioAnalysis, useFrequencyAnalysis } from './hooks/useAudioAnalysis';
import Spectrogram from './Spectrogram';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import { SectionTitle } from '../../common/Headings.tsx';
import AnalysisTab from '../AnalysisTab.tsx';

function AudioOverview() {
  const { selectedFolder: folder } = useBeatmap();
  const { settings } = useSettings();

  const { data, isLoading, isError, error } = useAudioAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  const { data: frequencyData, isLoading: frequencyLoading } = useFrequencyAnalysis({
    folder,
    songFolder: settings.songFolder,
  });

  const durationMs = data?.formatAnalysis?.durationMs || 0;

  return (
    <AnalysisTab data={data} isLoading={isLoading} isError={isError} error={error} subject="audio">
      {(data) => (
        <>
          <SimpleGrid cols={{ base: 1, lg: 2 }} spacing="md">
            {data.formatAnalysis && (
              <FormatInfo
                data={data.formatAnalysis}
                audioFilePath={data.audioFilePath}
                bitrateData={data.bitrateAnalysis}
              />
            )}
            {folder && <Spectrogram folder={folder} songFolder={settings.songFolder ?? ''} />}
          </SimpleGrid>

          {settings.showAdvancedAudioAnalysis && (
            <>
              <SectionTitle>Advanced audio analysis</SectionTitle>
              <SimpleGrid cols={{ base: 1, md: 2, lg: 3 }} spacing="md">
                {data.channelAnalysis && (
                  <ChannelBalance data={data.channelAnalysis} durationMs={durationMs} />
                )}
                {data.dynamicRangeAnalysis && (
                  <DynamicRange data={data.dynamicRangeAnalysis} durationMs={durationMs} />
                )}
                <FrequencyAnalysis data={frequencyData} isLoading={frequencyLoading} />
              </SimpleGrid>
            </>
          )}
        </>
      )}
    </AnalysisTab>
  );
}

export default AudioOverview;
