import { useQuery } from '@tanstack/react-query';
import { FetchError } from '../../../../client/ApiHelper';
import BeatmapAnalysisApi from '../../../../client/BeatmapAnalysisApi';
import { DifficultyOverviewResult } from '../../../../Types';

interface UseDifficultyOverviewArgs {
  folder?: string;
  songFolder?: string;
}

export function useDifficultyOverview({ folder, songFolder }: UseDifficultyOverviewArgs) {
  const beatmapFolderPath = folder && songFolder
    ? `${songFolder}\\${folder}`.replace(/\//g, '\\')
    : undefined;

  const query = useQuery<DifficultyOverviewResult, FetchError>({
    queryKey: ['difficulty-overview', beatmapFolderPath || 'unavailable'],
    queryFn: () => {
      if (!beatmapFolderPath) throw new Error('Beatmap folder path unavailable');
      return BeatmapAnalysisApi.getDifficultyOverview({ beatmapSetFolder: beatmapFolderPath });
    },
    enabled: !!beatmapFolderPath,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false,
  });

  return { ...query, beatmapFolderPath, refetch: query.refetch };
}