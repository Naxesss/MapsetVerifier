import { useQuery } from '@tanstack/react-query';
import { FetchError } from '../../../../client/ApiHelper';
import MetadataAnalysisApi from '../../../../client/MetadataAnalysisApi';
import { StoryboardDetailResult } from '../../../../Types';

/** What the storyboard is made of, read once the storyboard is picked: the images can be many. */
export function useStoryboardDetail(beatmapFolderPath: string | undefined) {
  return useQuery<StoryboardDetailResult, FetchError>({
    queryKey: ['storyboard-detail', beatmapFolderPath || 'unavailable'],
    queryFn: () => {
      if (!beatmapFolderPath) throw new Error('Beatmap folder path unavailable');
      return MetadataAnalysisApi.storyboard({ beatmapSetFolder: beatmapFolderPath });
    },
    // A server from before missing files were named sends no list; treat that as none named.
    select: (data) => ({
      ...data,
      images: (data.images ?? []).map((image) => ({
        ...image,
        missingFiles: image.missingFiles ?? [],
      })),
    }),
    enabled: !!beatmapFolderPath,
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    refetchOnMount: false,
  });
}
