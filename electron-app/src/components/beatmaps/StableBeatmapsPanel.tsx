import { Alert, Text } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import BeatmapCard from './BeatmapCard.tsx';
import CurrentBeatmapCard, { toCurrentBeatmap } from './CurrentBeatmapCard.tsx';
import MapsetListPanel, { type MapsetPageRequest } from './MapsetListPanel.tsx';
import { SongsFolderMissing } from './MapsetListParts.tsx';
import { FetchError } from '../../client/ApiHelper.ts';
import BeatmapApi from '../../client/BeatmapApi.ts';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { ApiLazerLookupResult } from '../../Types.ts';

interface Props {
  songFolder?: string;
  onOpenSettings: () => void;
}

/** The osu!(stable) mapsets in the Songs folder; bookmarks are filtered by the backend. */
export default function StableBeatmapsPanel({ songFolder, onOpenSettings }: Props) {
  const { selectedFolderPath, setSelectedFolderPath } = useBeatmap();

  const currentQuery = useQuery<ApiLazerLookupResult, FetchError>({
    queryKey: ['stable-current', songFolder || 'auto'],
    queryFn: () => BeatmapApi.getStableCurrent(songFolder),
    refetchOnWindowFocus: false,
    refetchInterval: 1800,
    retry: false,
  });
  const current = toCurrentBeatmap(currentQuery.data);

  const fetchPage = ({ page, pageSize, search, bookmarkedFolders }: MapsetPageRequest) => {
    const params = new URLSearchParams();
    if (songFolder) params.append('songsFolder', songFolder);
    if (search) params.append('search', search);
    if (bookmarkedFolders) params.append('bookmarkedFolders', bookmarkedFolders.join(','));
    params.append('page', String(page));
    params.append('pageSize', String(pageSize));
    return BeatmapApi.get(params);
  };

  return (
    <MapsetListPanel
      listKey={['beatmaps', songFolder]}
      fetchPage={fetchPage}
      listOptions={{ enabled: !!songFolder, staleTime: Infinity }}
      libraryName="osu! Songs folder"
      loadErrorMessage="The backend couldn't read your Songs folder."
      listHidden={!songFolder}
      notices={
        <>
          {!songFolder && <SongsFolderMissing onOpenSettings={onOpenSettings} />}
          {currentQuery.data?.status === 'ambiguous_client' && (
            <Alert
              icon={<IconAlertCircle />}
              color="yellow"
              title="Ambiguous osu! client"
              variant="light"
            >
              <Text size="sm">
                {currentQuery.data.message ??
                  'Could not confidently identify osu!stable while multiple osu! clients are open.'}
              </Text>
            </Alert>
          )}
        </>
      }
      hasCurrent={!!current}
      currentCard={
        <CurrentBeatmapCard
          current={current}
          selectedFolderPath={selectedFolderPath}
          onSelectFolderPath={setSelectedFolderPath}
        />
      }
      renderCard={(beatmap, index) => (
        <BeatmapCard
          key={beatmap.folder + beatmap.title}
          beatmap={beatmap}
          songFolder={songFolder}
          enterIndex={index}
        />
      )}
      onRefresh={() => void currentQuery.refetch()}
    />
  );
}
