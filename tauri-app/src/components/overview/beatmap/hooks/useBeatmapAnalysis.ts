import { useQuery } from '@tanstack/react-query';
import { FetchError } from '../../../../client/ApiHelper';
import BeatmapAnalysisApi from '../../../../client/BeatmapAnalysisApi';
import { BeatmapAnalysisResult } from '../../../../Types';
import { buildBeatmapFolderPath } from '../../../../utils/buildBeatmapFolderPath';

interface UseBeatmapAnalysisArgs {
  folder?: string;
  songFolder?: string;
}

export function useBeatmapAnalysis({ folder, songFolder }: UseBeatmapAnalysisArgs) {
  const beatmapFolderPath = buildBeatmapFolderPath(songFolder, folder);

  const query = useQuery<BeatmapAnalysisResult, FetchError>({
    queryKey: ['beatmap-analysis', beatmapFolderPath || 'unavailable'],
    queryFn: () => {
      if (!beatmapFolderPath) throw new Error('Beatmap folder path unavailable');
      return BeatmapAnalysisApi.analyze({ beatmapSetFolder: beatmapFolderPath });
    },
    enabled: !!beatmapFolderPath,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false,
  });

  return { ...query, beatmapFolderPath, refetch: query.refetch };
}

