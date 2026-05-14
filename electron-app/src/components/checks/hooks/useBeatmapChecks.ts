import { useQuery } from '@tanstack/react-query';
import { FetchError } from '../../../client/ApiHelper';
import BeatmapApi from '../../../client/BeatmapApi';
import { ApiBeatmapSetCheckResult } from '../../../Types';
import { buildBeatmapFolderPath } from '../../../utils/buildBeatmapFolderPath';

interface UseBeatmapChecksArgs {
  folder?: string;
  songFolder?: string;
}

export function useBeatmapChecks({ folder, songFolder }: UseBeatmapChecksArgs) {
  const beatmapFolderPath = buildBeatmapFolderPath(songFolder, folder);

  const query = useQuery<ApiBeatmapSetCheckResult, FetchError>({
    queryKey: ['beatmap-checks', beatmapFolderPath || 'unavailable'],
    queryFn: () => {
      if (!beatmapFolderPath) throw new Error('Beatmap folder path unavailable');
      return BeatmapApi.runChecks(beatmapFolderPath);
    },
    enabled: !!beatmapFolderPath,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false,
  });

  return { ...query, beatmapFolderPath, refetch: query.refetch };
}
