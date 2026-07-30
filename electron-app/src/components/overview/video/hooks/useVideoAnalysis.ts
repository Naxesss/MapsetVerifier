import { useQuery } from '@tanstack/react-query';
import { FetchError } from '../../../../client/ApiHelper';
import VideoAnalysisApi from '../../../../client/VideoAnalysisApi';
import { VideoAnalysisResult } from '../../../../Types';
import { buildBeatmapFolderPath } from '../../../../utils/buildBeatmapFolderPath';

interface UseVideoAnalysisArgs {
  folder?: string;
  songFolder?: string;
}

export function useVideoAnalysis({ folder, songFolder }: UseVideoAnalysisArgs) {
  const beatmapFolderPath = buildBeatmapFolderPath(songFolder, folder);

  const query = useQuery<VideoAnalysisResult, FetchError>({
    queryKey: ['video-analysis', beatmapFolderPath || 'unavailable'],
    queryFn: () => {
      if (!beatmapFolderPath) throw new Error('Beatmap folder path unavailable');
      return VideoAnalysisApi.analyze({ beatmapSetFolder: beatmapFolderPath });
    },
    enabled: !!beatmapFolderPath,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false,
  });

  return { ...query, beatmapFolderPath, refetch: query.refetch };
}
