import { Stack } from '@mantine/core';
import ChannelBalance from './ChannelBalance';
import DynamicRange from './DynamicRange';
import FrequencyAnalysis from './FrequencyAnalysis';
import { useAudioAnalysis, useFrequencyAnalysis } from './hooks/useAudioAnalysis';
import Spectrogram from './Spectrogram';
import { useBeatmap } from '../../../context/BeatmapContext.tsx';
import { useSettings } from '../../../context/SettingsContext.tsx';
import {
  buildBeatmapAudioUrl,
  buildBeatmapFolderPath,
} from '../../../utils/buildBeatmapFolderPath.ts';
import AudioWaveformPlayer from '../../common/audio/AudioWaveformPlayer.tsx';
import { SectionTitle } from '../../common/Headings.tsx';
import { StatField } from '../../common/StatField.tsx';
import AnalysisTab from '../AnalysisTab.tsx';
import FileCard from '../resources/FileCard.tsx';
import { KIND_COLORS } from '../resources/resourceFiles.ts';

const formatChannels = (channels: number) =>
  channels === 1 ? 'Mono' : channels === 2 ? 'Stereo' : `${channels}ch`;

const formatKbps = (kbps: number) => `${kbps.toLocaleString()} kbps`;

/**
 * The song: how it's stored and its waveform to play, its spectrogram, then, when turned on, the
 * advanced analysis.
 */
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
  const beatmapFolderPath = buildBeatmapFolderPath(settings.songFolder, folder);
  const audioUrl = beatmapFolderPath ? buildBeatmapAudioUrl(beatmapFolderPath) : null;

  return (
    <AnalysisTab
      data={data}
      isLoading={isLoading}
      isError={isError}
      error={error}
      subject="audio"
      padded={false}
    >
      {(data) => {
        const format = data.formatAnalysis;
        const bitrate = data.bitrateAnalysis;
        // A variable bitrate also gets its range; a constant one has none worth showing.
        const bitrateNote = !bitrate
          ? undefined
          : bitrate.isVbr && bitrate.minBitrate != null && bitrate.maxBitrate != null
            ? `Variable, ${bitrate.minBitrate.toLocaleString()}–${bitrate.maxBitrate.toLocaleString()} kbps`
            : bitrate.isVbr
              ? 'Variable'
              : 'Constant';
        return (
          <>
            <FileCard
              title="Sound"
              stats={
                <>
                  <StatField label="Length" value={format?.durationFormatted || 'Unknown'} />
                  <StatField
                    label="Bitrate"
                    value={bitrate ? formatKbps(bitrate.averageBitrate) : 'Unknown'}
                    note={bitrateNote}
                  />
                  <StatField label="Codec" value={format?.codec || 'Unknown'} />
                  <StatField
                    label="Sample rate"
                    value={format ? `${(format.sampleRate / 1000).toFixed(1)} kHz` : 'Unknown'}
                  />
                  <StatField
                    label="Channels"
                    value={format ? formatChannels(format.channels) : 'Unknown'}
                  />
                  {format && format.bitDepth > 0 && (
                    <StatField label="Bit depth" value={`${format.bitDepth}-bit`} />
                  )}
                </>
              }
            >
              {audioUrl && (
                <AudioWaveformPlayer url={audioUrl} color={KIND_COLORS.audio} height={120} />
              )}
            </FileCard>

            {folder && <Spectrogram folder={folder} songFolder={settings.songFolder ?? ''} />}

            {settings.showAdvancedAudioAnalysis && (
              <>
                <SectionTitle>Advanced audio analysis</SectionTitle>
                <Stack gap="md">
                  {data.channelAnalysis && (
                    <ChannelBalance data={data.channelAnalysis} durationMs={durationMs} />
                  )}
                  {data.dynamicRangeAnalysis && (
                    <DynamicRange data={data.dynamicRangeAnalysis} durationMs={durationMs} />
                  )}
                  <FrequencyAnalysis data={frequencyData} isLoading={frequencyLoading} />
                </Stack>
              </>
            )}
          </>
        );
      }}
    </AnalysisTab>
  );
}

export default AudioOverview;
