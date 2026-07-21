import {
  Alert,
  CloseButton,
  Collapse,
  Divider,
  Flex,
  TextInput,
  Button,
  Text,
  ScrollArea,
  ActionIcon,
  Tooltip,
} from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import {
  IconAlertCircle,
  IconListDetails,
  IconPin,
  IconPinFilled,
  IconRefresh,
  IconSearchOff,
  IconSettings,
} from '@tabler/icons-react';
import { useInfiniteQuery, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useRef, useState } from 'react';
import BeatmapCard from './BeatmapCard.tsx';
import CurrentBeatmapCard, { CurrentBeatmapData } from './CurrentBeatmapCard.tsx';
import PlaceholderBeatmapCard from './PlaceholderBeatmapCard.tsx';
import { FetchError } from '../../client/ApiHelper.ts';
import BeatmapApi from '../../client/BeatmapApi.ts';
import { useBeatmap } from '../../context/BeatmapContext.tsx';
import { useSettings } from '../../context/SettingsContext.tsx';
import { ApiBeatmapPage, ApiLazerLookupResult, Beatmap } from '../../Types.ts';
import { lazerBackgroundVersion } from '../../utils/buildBeatmapFolderPath.ts';

interface Props {
  lazerDataDir?: string;
  onOpenSettings: () => void;
}

function backgroundVersionTicks(backgroundPath?: string): bigint | null {
  const version = lazerBackgroundVersion(backgroundPath);
  if (!version) return null;
  try {
    return BigInt(`0x${version}`);
  } catch {
    return null;
  }
}

export default function LazerBeatmapsPanel({ lazerDataDir, onOpenSettings }: Props) {
  const {
    selectedFolderPath,
    lazerSourceSetId,
    setSelectedFolderPath,
    setSelectedLazerFolderPath,
  } = useBeatmap();
  const { settings } = useSettings();
  const [search, setSearch] = useState('');
  const [debouncedSearch] = useDebouncedValue(search, 300);
  const [bookmarkedOnly, setBookmarkedOnly] = useState(false);
  const [materializingSetId, setMaterializingSetId] = useState<string | undefined>(undefined);
  const [materializeError, setMaterializeError] = useState<string | undefined>(undefined);
  const sentinelRef = useRef<HTMLDivElement | null>(null);
  const scrollRef = useRef<HTMLDivElement | null>(null);
  const queryClient = useQueryClient();
  const stepSize = 16;

  const bookmarkedFolders = settings.bookmarkedFolders;
  const filterByBookmarks = bookmarkedOnly && settings.bookmarksEnabled;
  const hasNoBookmarks = filterByBookmarks && bookmarkedFolders.length === 0;

  const { data, error, isFetching, isFetchingNextPage, fetchNextPage, hasNextPage, refetch } =
    useInfiniteQuery<ApiBeatmapPage, FetchError>({
      queryKey: [
        'lazer-beatmaps',
        lazerDataDir,
        debouncedSearch,
        stepSize,
        filterByBookmarks ? bookmarkedFolders : null,
      ],
      enabled: !hasNoBookmarks,
      initialPageParam: 0,
      queryFn: async ({ pageParam }) => {
        const params = new URLSearchParams();
        if (lazerDataDir) params.append('lazerDataDir', lazerDataDir);
        if (debouncedSearch) params.append('search', debouncedSearch);
        params.append('page', String(pageParam));
        params.append('pageSize', stepSize.toString());
        try {
          const page = await BeatmapApi.getLazerList(params);
          if (filterByBookmarks) {
            return {
              ...page,
              items: page.items.filter((bm) => bookmarkedFolders.includes(bm.folder)),
            };
          }
          return page;
        } catch (err) {
          if (err instanceof FetchError && err.res?.status === 404) {
            return { items: [], page: Number(pageParam), pageSize: stepSize, hasMore: false };
          }
          throw err;
        }
      },
      getNextPageParam: (lastPage) => (lastPage.hasMore ? lastPage.page + 1 : undefined),
      // List metadata is refreshed on card click / F5 / focus — not on a slow timer.
      staleTime: 0,
      refetchOnWindowFocus: true,
      retry: (failureCount, queryError) => {
        if (queryError.res?.status === 404) return false;
        return failureCount < 2;
      },
    });

  const lazerCurrentQuery = useQuery<ApiLazerLookupResult, FetchError>({
    queryKey: ['lazer-current', lazerDataDir || 'auto'],
    queryFn: () => BeatmapApi.getLazerCurrent(lazerDataDir),
    enabled: true,
    refetchOnWindowFocus: false,
    refetchInterval: (query) => (query.state.data?.status === 'folder_found' ? 5000 : 1500),
    retry: false,
  });

  const beatmaps: Beatmap[] = data?.pages.flatMap((p) => p.items) ?? [];
  const firstPageLoaded = data?.pages?.[0];
  const noResults = !isFetching && !isFetchingNextPage && beatmaps.length === 0 && !error;
  const lastPage = data?.pages[data.pages.length - 1];
  const showNextPagePlaceholder =
    isFetchingNextPage || (lastPage && lastPage.items.length === 0 && lastPage.hasMore);

  const lazerCurrentResult: CurrentBeatmapData | null = useMemo(() => {
    if (
      lazerCurrentQuery.data &&
      lazerCurrentQuery.data.status === 'folder_found' &&
      lazerCurrentQuery.data.beatmap &&
      lazerCurrentQuery.data.folderPath &&
      lazerCurrentQuery.data.lookupRoot
    ) {
      return {
        beatmap: lazerCurrentQuery.data.beatmap,
        folderPath: lazerCurrentQuery.data.folderPath,
        lookupRoot: lazerCurrentQuery.data.lookupRoot,
      };
    }

    return null;
  }, [lazerCurrentQuery.data]);

  // Keep the matching list card in sync with Current as soon as the poll sees a change.
  useEffect(() => {
    const currentBeatmap = lazerCurrentResult?.beatmap;
    if (!currentBeatmap) return;

    queryClient.setQueriesData<{ pages: ApiBeatmapPage[]; pageParams: unknown[] }>(
      { queryKey: ['lazer-beatmaps'] },
      (old) => {
        if (!old?.pages) return old;

        let changed = false;
        const pages = old.pages.map((page) => {
          const items = page.items.map((bm) => {
            if (bm.folder !== currentBeatmap.folder) return bm;

            const listVersion = backgroundVersionTicks(bm.backgroundPath);
            const currentVersion = backgroundVersionTicks(currentBeatmap.backgroundPath);
            // Never let a stale Current poll regress a fresher list card (e.g. right after F5).
            if (listVersion != null && currentVersion != null && currentVersion < listVersion) {
              return bm;
            }

            if (
              bm.title === currentBeatmap.title &&
              bm.artist === currentBeatmap.artist &&
              bm.creator === currentBeatmap.creator &&
              bm.backgroundPath === currentBeatmap.backgroundPath
            ) {
              return bm;
            }
            changed = true;
            return { ...bm, ...currentBeatmap };
          });
          return items === page.items ? page : { ...page, items };
        });

        return changed ? { ...old, pages } : old;
      }
    );
  }, [lazerCurrentResult?.beatmap, queryClient]);

  useEffect(() => {
    if (!lastPage) return;
    if (lastPage.items.length === 0 && lastPage.hasMore && !isFetchingNextPage) {
      fetchNextPage();
    }
  }, [lastPage, isFetchingNextPage, fetchNextPage]);

  useEffect(() => {
    const el = sentinelRef.current;
    if (!el || !hasNextPage) return;
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasNextPage && !isFetchingNextPage) {
          fetchNextPage();
        }
      },
      { root: null, rootMargin: '200px', threshold: 0.1 }
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, [hasNextPage, isFetchingNextPage, fetchNextPage]);

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: 0, behavior: 'auto' });
  }, [debouncedSearch]);

  const refreshLazerList = () =>
    queryClient.invalidateQueries({ queryKey: ['lazer-beatmaps'], refetchType: 'active' });

  const selectLazerBeatmap = async (setId: string) => {
    if (materializingSetId) return;

    setMaterializeError(undefined);
    setMaterializingSetId(setId);
    try {
      // Pull fresh list metadata immediately on open so the card matches realm/Current.
      void refreshLazerList();

      const result = await BeatmapApi.materializeLazer(setId, lazerDataDir);
      if (result.success && result.folderPath) {
        setSelectedLazerFolderPath(setId, result.folderPath);
        // Path is stable across rematerializes, so invalidate so checks/overview refetch.
        await queryClient.invalidateQueries({
          predicate: (query) =>
            Array.isArray(query.queryKey) &&
            query.queryKey.length >= 2 &&
            query.queryKey[1] === result.folderPath,
          refetchType: 'all',
        });
      } else {
        setMaterializeError(result.errorMessage ?? 'Failed to open this beatmapset.');
      }
    } catch (err) {
      setMaterializeError(
        err instanceof FetchError ? err.message : 'Failed to open this beatmapset.'
      );
    } finally {
      setMaterializingSetId(undefined);
    }
  };

  const renderTopStatus = () => {
    if (materializeError) {
      return (
        <Alert icon={<IconAlertCircle />} color="red" title="Could not open beatmapset" mt="xs">
          <Text>{materializeError}</Text>
        </Alert>
      );
    }

    if (lazerCurrentQuery.data?.status === 'lazer_data_dir_not_found') {
      return (
        <Alert
          icon={<IconAlertCircle />}
          color="yellow"
          title="Lazer data folder not found"
          mt="xs"
          variant="light"
        >
          <Text size="sm" mb="xs">
            {lazerCurrentQuery.data?.message ?? 'Could not detect your osu!(lazer) data folder.'}
          </Text>
          <Button
            size="xs"
            variant="light"
            color="gray"
            leftSection={<IconSettings />}
            onClick={onOpenSettings}
          >
            Open settings
          </Button>
        </Alert>
      );
    }

    if (showNextPagePlaceholder) return null;

    if (hasNoBookmarks) {
      return (
        <Alert icon={<IconPin />} color="gray" title="No bookmarks yet" mt="xs" variant="light">
          Pin a beatmapset from the list to find it here quickly.
        </Alert>
      );
    }

    if (error) {
      const status = error.res?.status;
      const msg =
        status === 404
          ? debouncedSearch
            ? 'The search yielded no results.'
            : filterByBookmarks
              ? 'None of your bookmarks match the current filters.'
              : 'No mapsets could be found in your lazer library.'
          : error.message || 'Failed to load beatmaps.';
      return (
        <Alert icon={<IconAlertCircle />} color="red" title="Error" mt="xs">
          <Text>{msg}</Text>
          <Button size="xs" variant="light" color="red" onClick={() => refetch()}>
            Retry
          </Button>
        </Alert>
      );
    }

    if (noResults) {
      return (
        <Alert icon={<IconSearchOff />} color="gray" title="No results" mt="xs" variant="light">
          {debouncedSearch
            ? 'The search yielded no results.'
            : filterByBookmarks
              ? 'None of your bookmarks match the current filters.'
              : 'No mapsets could be found in your lazer library.'}
        </Alert>
      );
    }

    return null;
  };

  const renderLazerCurrentMap = () => (
    <Collapse in={!!lazerCurrentResult}>
      <Text size="xs" fw={500} my="sm" ml="sm" c="dimmed">
        Current mapset
      </Text>
      <CurrentBeatmapCard
        current={lazerCurrentResult}
        selectedFolderPath={selectedFolderPath}
        source="lazer"
        lazerDataDir={lazerDataDir}
        onSelectFolderPath={(folderPath) => {
          if (folderPath && lazerCurrentResult) {
            // Rematerialize instead of trusting the poll's cached folder path.
            void selectLazerBeatmap(lazerCurrentResult.beatmap.folder);
          } else {
            setSelectedFolderPath(folderPath);
          }
        }}
      />
      <Divider my="sm" />
    </Collapse>
  );

  return (
    <>
      <Flex direction="column" gap="sm" p="xs">
        <Flex gap="sm" direction="row" justify="space-between">
          <TextInput
            placeholder="Search beatmaps..."
            value={search}
            onChange={(e) => {
              const value = e.target.value;
              if (value === '' && search !== '') {
                scrollRef.current?.scrollTo({ top: 0, behavior: 'auto' });
              }
              setSearch(value);
            }}
            rightSectionPointerEvents="all"
            rightSection={
              <CloseButton
                aria-label="Clear input"
                onClick={() => {
                  if (search !== '') {
                    scrollRef.current?.scrollTo({ top: 0, behavior: 'auto' });
                  }
                  setSearch('');
                }}
                style={{ display: search ? undefined : 'none' }}
              />
            }
          />
          {settings.bookmarksEnabled && (
            <Tooltip label={bookmarkedOnly ? 'Show all beatmapsets' : 'Show bookmarked only'}>
              <ActionIcon
                variant={bookmarkedOnly ? 'light' : 'default'}
                color="yellow"
                onClick={() => setBookmarkedOnly((prev) => !prev)}
                size="36"
                aria-label={bookmarkedOnly ? 'Show all beatmapsets' : 'Show bookmarked only'}
              >
                {bookmarkedOnly ? <IconPinFilled /> : <IconPin />}
              </ActionIcon>
            </Tooltip>
          )}
          <Tooltip label="Refresh beatmap search">
            <ActionIcon
              variant="default"
              onClick={() => {
                queryClient.resetQueries({ queryKey: ['lazer-beatmaps'] });
                lazerCurrentQuery.refetch();
              }}
              size="36"
            >
              <IconRefresh />
            </ActionIcon>
          </Tooltip>
        </Flex>
        {renderTopStatus()}
      </Flex>
      {!error && !hasNoBookmarks && (
        <>
          <Divider />
          <ScrollArea
            type="always"
            scrollbars="y"
            offsetScrollbars="y"
            viewportRef={scrollRef}
            p="xs"
            style={{ flex: '1 1 auto' }}
          >
            <Flex direction="column" gap="xs" w="100%" style={{ justifyContent: 'center' }}>
              {renderLazerCurrentMap()}
              {!firstPageLoaded &&
                Array.from({ length: 6 }).map((_, i) => <PlaceholderBeatmapCard key={i} />)}
              {beatmaps.map((bm, i) => (
                <BeatmapCard
                  key={bm.folder + bm.title}
                  beatmap={bm}
                  source="lazer"
                  lazerDataDir={lazerDataDir}
                  isSelectedOverride={lazerSourceSetId === bm.folder}
                  onSelect={() => selectLazerBeatmap(bm.folder)}
                  enterIndex={i}
                />
              ))}
              <div ref={sentinelRef} style={{ height: 1 }} />
              {showNextPagePlaceholder && <PlaceholderBeatmapCard />}
              {beatmaps.length > 0 &&
                !hasNextPage &&
                !isFetchingNextPage &&
                !error &&
                !filterByBookmarks && (
                  <Alert
                    icon={<IconListDetails />}
                    color="gray"
                    title="No more beatmaps"
                    variant="light"
                  >
                    You have reached the last available beatmap.
                    <Button
                      size="xs"
                      mt="xs"
                      variant="default"
                      onClick={() => scrollRef.current?.scrollTo({ top: 0, behavior: 'smooth' })}
                    >
                      Back to top
                    </Button>
                  </Alert>
                )}
            </Flex>
          </ScrollArea>
        </>
      )}
    </>
  );
}
