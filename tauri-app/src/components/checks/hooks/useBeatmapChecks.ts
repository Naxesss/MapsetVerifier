import { useQuery } from '@tanstack/react-query';
import { FetchError } from '../../../client/ApiHelper';
import BeatmapApi from '../../../client/BeatmapApi';
import { ApiBeatmapSetCheckResult } from '../../../Types';

interface UseBeatmapChecksArgs {
  folder?: string;
  songFolder?: string;
}

export function useBeatmapChecks({ folder, songFolder }: UseBeatmapChecksArgs) {
  // Normalize windows path (replace forward slashes) only when both are defined.
  const beatmapFolderPath = folder && songFolder
    ? `${songFolder}\\${folder}`.replace(/\//g, '\\')
    : undefined;

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
