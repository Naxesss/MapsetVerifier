import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { FetchError } from '../../../client/ApiHelper';
import SnapshotApi from '../../../client/SnapshotApi';
import { ApiSnapshotHistory } from '../../../Types';
import { buildBeatmapFolderPath } from '../../../utils/buildBeatmapFolderPath';

interface UseSnapshotHistoryArgs {
  folder?: string;
  songFolder?: string;
}

/**
 * The mapset's snapshots, newest first. Opening the page takes a snapshot of the mapset as it is
 * now, so the latest entry is always the current state.
 */
export function useSnapshotHistory({ folder, songFolder }: UseSnapshotHistoryArgs) {
  const beatmapFolderPath = buildBeatmapFolderPath(songFolder, folder);
  const queryClient = useQueryClient();
  const queryKey = ['snapshot-history', beatmapFolderPath || 'unavailable'];

  const query = useQuery<ApiSnapshotHistory, FetchError>({
    queryKey,
    queryFn: () => {
      if (!beatmapFolderPath) throw new Error('Beatmap folder path unavailable');
      return SnapshotApi.getHistory(beatmapFolderPath);
    },
    enabled: !!beatmapFolderPath,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
  });

  const setHistory = (history: ApiSnapshotHistory) => queryClient.setQueryData(queryKey, history);

  const pin = useMutation<ApiSnapshotHistory, FetchError, { id: string; pin: string | null }>({
    mutationFn: ({ id, pin: name }) => {
      const setKey = query.data?.setKey;
      if (!setKey) throw new Error('No snapshots to pin');
      return SnapshotApi.setPin(setKey, id, name);
    },
    onSuccess: setHistory,
  });

  return { ...query, beatmapFolderPath, pin };
}
