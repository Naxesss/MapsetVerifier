import { useQuery } from '@tanstack/react-query';
import { FetchError } from '../../../client/ApiHelper';
import AudioAnalysisApi from '../../../client/AudioAnalysisApi';
import { AudioAnalysisResult, SpectralAnalysisResult, FrequencyAnalysisResult, HitSoundBatchResult } from '../../../Types';

interface UseAudioAnalysisArgs {
  folder?: string;
  songFolder?: string;
}

export function useAudioAnalysis({ folder, songFolder }: UseAudioAnalysisArgs) {
  const beatmapFolderPath = folder && songFolder
    ? `${songFolder}\\${folder}`.replace(/\//g, '\\')
    : undefined;

  const query = useQuery<AudioAnalysisResult, FetchError>({
    queryKey: ['audio-analysis', beatmapFolderPath || 'unavailable'],
    queryFn: () => {
      if (!beatmapFolderPath) throw new Error('Beatmap folder path unavailable');
      return AudioAnalysisApi.analyze({ beatmapSetFolder: beatmapFolderPath });
    },
    enabled: !!beatmapFolderPath,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false,
  });

  return { ...query, beatmapFolderPath, refetch: query.refetch };
}

export function useSpectrogram({ folder, songFolder }: UseAudioAnalysisArgs) {
  const beatmapFolderPath = folder && songFolder
    ? `${songFolder}\\${folder}`.replace(/\//g, '\\')
    : undefined;

  const query = useQuery<SpectralAnalysisResult, FetchError>({
    queryKey: ['spectrogram', beatmapFolderPath || 'unavailable'],
    queryFn: () => {
      if (!beatmapFolderPath) throw new Error('Beatmap folder path unavailable');
      return AudioAnalysisApi.getSpectrogram({
        beatmapSetFolder: beatmapFolderPath,
        fftSize: 2048,
        timeResolutionMs: 50,
      });
    },
    enabled: !!beatmapFolderPath,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false,
  });

  return { ...query, beatmapFolderPath };
}

export function useFrequencyAnalysis({ folder, songFolder }: UseAudioAnalysisArgs) {
  const beatmapFolderPath = folder && songFolder
    ? `${songFolder}\\${folder}`.replace(/\//g, '\\')
    : undefined;

  const query = useQuery<FrequencyAnalysisResult, FetchError>({
    queryKey: ['frequency-analysis', beatmapFolderPath || 'unavailable'],
    queryFn: () => {
      if (!beatmapFolderPath) throw new Error('Beatmap folder path unavailable');
      return AudioAnalysisApi.getFrequencyAnalysis({ 
        beatmapSetFolder: beatmapFolderPath,
        fftSize: 4096,
      });
    },
    enabled: !!beatmapFolderPath,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false,
  });

  return { ...query, beatmapFolderPath };
}

export function useHitSoundAnalysis({ folder, songFolder }: UseAudioAnalysisArgs) {
  const beatmapFolderPath = folder && songFolder
    ? `${songFolder}\\${folder}`.replace(/\//g, '\\')
    : undefined;

  const query = useQuery<HitSoundBatchResult, FetchError>({
    queryKey: ['hitsound-analysis', beatmapFolderPath || 'unavailable'],
    queryFn: () => {
      if (!beatmapFolderPath) throw new Error('Beatmap folder path unavailable');
      return AudioAnalysisApi.analyzeHitSounds({ beatmapSetFolder: beatmapFolderPath });
    },
    enabled: !!beatmapFolderPath,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false,
  });

  return { ...query, beatmapFolderPath };
}

