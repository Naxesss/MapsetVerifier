import { useQuery } from '@tanstack/react-query';
import { useEffect, useRef, useState } from 'react';
import { FetchError } from '../../../client/ApiHelper';
import BeatmapApi from '../../../client/BeatmapApi';
import { ApiBeatmapSetCheckResult, ApiBeatmapStructure, CheckProgress } from '../../../Types';
import { buildBeatmapFolderPath } from '../../../utils/buildBeatmapFolderPath';

interface UseBeatmapChecksArgs {
  folder?: string;
  songFolder?: string;
  includeCheckRunDelta?: boolean;
  createSnapshot?: boolean;
}

export function useBeatmapChecks({
  folder,
  songFolder,
  includeCheckRunDelta = true,
  createSnapshot = true,
}: UseBeatmapChecksArgs) {
  const beatmapFolderPath = buildBeatmapFolderPath(songFolder, folder);
  const [progress, setProgress] = useState<CheckProgress | null>(null);
  const [structure, setStructure] = useState<ApiBeatmapStructure | null>(null);
  const activeRequestPathRef = useRef<string | null>(null);

  useEffect(() => {
    activeRequestPathRef.current = beatmapFolderPath ?? null;
    setProgress(null);
    setStructure(null);
  }, [beatmapFolderPath]);

  const query = useQuery<ApiBeatmapSetCheckResult, FetchError>({
    queryKey: [
      'beatmap-checks',
      beatmapFolderPath || 'unavailable',
      includeCheckRunDelta,
      createSnapshot,
    ],
    queryFn: ({ signal }) => {
      if (!beatmapFolderPath) throw new Error('Beatmap folder path unavailable');

      const requestPath = beatmapFolderPath;
      activeRequestPathRef.current = requestPath;

      const isCurrentRequest = () => activeRequestPathRef.current === requestPath;

      setProgress(null);
      setStructure(null);

      return BeatmapApi.runChecksStream(beatmapFolderPath, {
        includeCheckRunDelta,
        createSnapshot,
        signal,
        onProgress: (update) => {
          if (!isCurrentRequest()) return;
          setProgress(update);
        },
        onStructure: (update) => {
          if (!isCurrentRequest()) return;
          setStructure(update);
        },
      });
    },
    enabled: !!beatmapFolderPath,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false,
  });

  return {
    ...query,
    beatmapFolderPath,
    progress,
    structure,
    refetch: query.refetch,
  };
}
