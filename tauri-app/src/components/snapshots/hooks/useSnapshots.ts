import { useQuery } from '@tanstack/react-query';
import { FetchError } from '../../../client/ApiHelper';
import SnapshotApi from '../../../client/SnapshotApi';
import { ApiSnapshotResult } from '../../../Types';

interface UseSnapshotsArgs {
  folder?: string;
  songFolder?: string;
}

export function useSnapshots({ folder, songFolder }: UseSnapshotsArgs) {
  // Normalize windows path (replace forward slashes) only when both are defined.
  const beatmapFolderPath = folder && songFolder
    ? `${songFolder}\\${folder}`.replace(/\//g, '\\')
    : undefined;

  const query = useQuery<ApiSnapshotResult, FetchError>({
    queryKey: ['snapshots', beatmapFolderPath || 'unavailable'],
    queryFn: () => {
      if (!beatmapFolderPath) throw new Error('Beatmap folder path unavailable');
      return SnapshotApi.getSnapshots(beatmapFolderPath);
    },
    enabled: !!beatmapFolderPath,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false,
  });

  return { ...query, beatmapFolderPath, refetch: query.refetch };
}

