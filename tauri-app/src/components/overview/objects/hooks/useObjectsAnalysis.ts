import { useQuery } from '@tanstack/react-query';
import { FetchError } from '../../../../client/ApiHelper';
import ObjectsAnalysisApi from '../../../../client/ObjectsAnalysisApi';
import { ObjectsOverviewResult } from '../../../../Types';

interface UseObjectsAnalysisArgs {
  folder?: string;
  songFolder?: string;
}

export function useObjectsAnalysis({ folder, songFolder }: UseObjectsAnalysisArgs) {
  const beatmapFolderPath = folder && songFolder
    ? `${songFolder}\\${folder}`.replace(/\//g, '\\')
    : undefined;

  const query = useQuery<ObjectsOverviewResult, FetchError>({
    queryKey: ['objects-analysis', beatmapFolderPath || 'unavailable'],
    queryFn: () => {
      if (!beatmapFolderPath) throw new Error('Beatmap folder path unavailable');
      return ObjectsAnalysisApi.analyze({ beatmapSetFolder: beatmapFolderPath });
    },
    enabled: !!beatmapFolderPath,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false,
  });

  return { ...query, beatmapFolderPath, refetch: query.refetch };
}