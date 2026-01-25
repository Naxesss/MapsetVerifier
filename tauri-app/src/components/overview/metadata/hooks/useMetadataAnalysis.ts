import { useQuery } from '@tanstack/react-query';
import { FetchError } from '../../../../client/ApiHelper';
import MetadataAnalysisApi from '../../../../client/MetadataAnalysisApi';
import { MetadataAnalysisResult } from '../../../../Types';

interface UseMetadataAnalysisArgs {
  folder?: string;
  songFolder?: string;
}

export function useMetadataAnalysis({ folder, songFolder }: UseMetadataAnalysisArgs) {
  const beatmapFolderPath = folder && songFolder
    ? `${songFolder}\\${folder}`.replace(/\//g, '\\')
    : undefined;

  const query = useQuery<MetadataAnalysisResult, FetchError>({
    queryKey: ['metadata-analysis', beatmapFolderPath || 'unavailable'],
    queryFn: () => {
      if (!beatmapFolderPath) throw new Error('Beatmap folder path unavailable');
      return MetadataAnalysisApi.analyze({ beatmapSetFolder: beatmapFolderPath });
    },
    enabled: !!beatmapFolderPath,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false,
  });

  return { ...query, beatmapFolderPath, refetch: query.refetch };
}

