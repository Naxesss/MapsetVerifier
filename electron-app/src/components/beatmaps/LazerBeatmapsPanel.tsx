import { Alert, Button, Text } from '@mantine/core';
import { IconAlertCircle, IconSettings } from '@tabler/icons-react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import BeatmapCard from './BeatmapCard.tsx';
import CurrentBeatmapCard, { toCurrentBeatmap } from './CurrentBeatmapCard.tsx';
import MapsetListPanel, { type MapsetPageRequest } from './MapsetListPanel.tsx';
import { FetchError } from '../../client/ApiHelper.ts';
import BeatmapApi from '../../client/BeatmapApi.ts';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { ApiLazerLookupResult } from '../../Types.ts';

interface Props {
  lazerDataDir?: string;
  onOpenSettings: () => void;
}

/**
 * The osu!(lazer) mapsets from its realm database. Opening one first copies ("materializes") it to
 * a folder the checks can read; bookmarks are filtered here, as the backend doesn't know them.
 */
export default function LazerBeatmapsPanel({ lazerDataDir, onOpenSettings }: Props) {
  const {
    selectedFolderPath,
    lazerSourceSetId,
    lazerSourceOnlineSetId,
    setSelectedFolderPath,
    setSelectedLazerFolderPath,
  } = useBeatmap();
  const queryClient = useQueryClient();
  const [materializingSetId, setMaterializingSetId] = useState<string | undefined>(undefined);
  const [materializeError, setMaterializeError] = useState<string | undefined>(undefined);

  const currentQuery = useQuery<ApiLazerLookupResult, FetchError>({
    queryKey: ['lazer-current', lazerDataDir || 'auto'],
    queryFn: () => BeatmapApi.getLazerCurrent(lazerDataDir),
    refetchOnWindowFocus: false,
    refetchInterval: (query) => (query.state.data?.status === 'folder_found' ? 5000 : 1500),
    retry: false,
  });
  const current = toCurrentBeatmap(currentQuery.data);

  const fetchPage = async ({ page, pageSize, search, bookmarkedFolders }: MapsetPageRequest) => {
    const params = new URLSearchParams();
    if (lazerDataDir) params.append('lazerDataDir', lazerDataDir);
    if (search) params.append('search', search);
    params.append('page', String(page));
    params.append('pageSize', String(pageSize));
    const result = await BeatmapApi.getLazerList(params);
    return bookmarkedFolders
      ? { ...result, items: result.items.filter((bm) => bookmarkedFolders.includes(bm.folder)) }
      : result;
  };

  const selectLazerBeatmap = async (setId: string, onlineSetId?: string) => {
    if (materializingSetId) return;

    setMaterializeError(undefined);
    setMaterializingSetId(setId);
    try {
      // Pull fresh list metadata immediately on open so the card matches realm/Current.
      void queryClient.invalidateQueries({ queryKey: ['lazer-beatmaps'], refetchType: 'active' });

      const result = await BeatmapApi.materializeLazer(setId, lazerDataDir);
      if (result.success && result.folderPath) {
        const resolvedSetId = result.beatmapSetId || setId;
        setSelectedLazerFolderPath(resolvedSetId, result.folderPath, onlineSetId);
        // Path is stable across rematerializes, so invalidate so checks/overview refetch.
        await queryClient.invalidateQueries({
          predicate: (query) =>
            Array.isArray(query.queryKey) &&
            query.queryKey.length >= 2 &&
            query.queryKey[1] === result.folderPath,
          refetchType: 'all',
        });
      } else {
        setMaterializeError(result.errorMessage ?? "Couldn't open this mapset.");
      }
    } catch (err) {
      setMaterializeError(err instanceof FetchError ? err.message : "Couldn't open this mapset.");
    } finally {
      setMaterializingSetId(undefined);
    }
  };

  return (
    <MapsetListPanel
      listKey={['lazer-beatmaps', lazerDataDir]}
      fetchPage={fetchPage}
      // Refreshed on card click / F5 / focus — not polled continuously.
      listOptions={{ staleTime: 30_000, refetchOnWindowFocus: true }}
      libraryName="osu!(lazer) library"
      loadErrorMessage="The backend couldn't read your osu!(lazer) library."
      notices={
        <>
          {materializeError && (
            <Alert icon={<IconAlertCircle />} color="red" title="Couldn't open mapset">
              <Text size="sm">{materializeError}</Text>
            </Alert>
          )}
          {currentQuery.data?.status === 'lazer_data_dir_not_found' && (
            <Alert
              icon={<IconAlertCircle />}
              color="yellow"
              title="osu!(lazer) data folder not found"
              variant="light"
            >
              <Text size="sm" mb="xs">
                {currentQuery.data.message ?? 'Could not detect your osu!(lazer) data folder.'}
              </Text>
              <Button
                size="xs"
                variant="light"
                color="gray"
                leftSection={<IconSettings size={16} />}
                onClick={onOpenSettings}
              >
                Open settings
              </Button>
            </Alert>
          )}
        </>
      }
      hasCurrent={!!current}
      currentCard={
        <CurrentBeatmapCard
          current={current}
          selectedFolderPath={selectedFolderPath}
          source="lazer"
          lazerDataDir={lazerDataDir}
          onSelectFolderPath={(folderPath) => {
            if (folderPath && current) {
              // Rematerialize instead of trusting the poll's cached folder path.
              void selectLazerBeatmap(current.beatmap.folder, current.beatmap.beatmapSetID);
            } else {
              setSelectedFolderPath(folderPath);
            }
          }}
        />
      }
      renderCard={(beatmap, index) => (
        <BeatmapCard
          key={beatmap.folder + beatmap.title}
          beatmap={beatmap}
          source="lazer"
          lazerDataDir={lazerDataDir}
          isSelectedOverride={
            lazerSourceSetId === beatmap.folder ||
            (!!lazerSourceOnlineSetId &&
              !!beatmap.beatmapSetID &&
              beatmap.beatmapSetID === lazerSourceOnlineSetId)
          }
          onSelect={() => selectLazerBeatmap(beatmap.folder, beatmap.beatmapSetID)}
          enterIndex={index}
        />
      )}
      onRefresh={() => void currentQuery.refetch()}
    />
  );
}
