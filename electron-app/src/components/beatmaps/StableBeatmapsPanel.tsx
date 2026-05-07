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
import { ApiBeatmapPage, ApiLazerLookupResult, Beatmap } from '../../Types.ts';

interface Props {
  songFolder?: string;
  onOpenSettings: () => void;
}

export default function StableBeatmapsPanel({ songFolder, onOpenSettings }: Props) {
  const { selectedFolderPath, setSelectedFolderPath } = useBeatmap();
  const [search, setSearch] = useState('');
  const [debouncedSearch] = useDebouncedValue(search, 300);
  const sentinelRef = useRef<HTMLDivElement | null>(null);
  const scrollRef = useRef<HTMLDivElement | null>(null);
  const queryClient = useQueryClient();
  const stepSize = 16;

  const { data, error, isFetching, isFetchingNextPage, fetchNextPage, hasNextPage, refetch } =
    useInfiniteQuery<ApiBeatmapPage, FetchError>({
      queryKey: ['beatmaps', songFolder, debouncedSearch, stepSize],
      enabled: !!songFolder,
      initialPageParam: 0,
      queryFn: async ({ pageParam }) => {
        const params = new URLSearchParams();
        if (songFolder) params.append('songsFolder', songFolder);
        if (debouncedSearch) params.append('search', debouncedSearch);
        params.append('page', String(pageParam));
        params.append('pageSize', stepSize.toString());
        try {
          return await BeatmapApi.get(params);
        } catch (err) {
          if (err instanceof FetchError && err.res?.status === 404) {
            return { items: [], page: Number(pageParam), pageSize: stepSize, hasMore: false };
          }
          throw err;
        }
      },
      getNextPageParam: (lastPage) => (lastPage.hasMore ? lastPage.page + 1 : undefined),
      staleTime: Infinity,
      retry: (failureCount, queryError) => {
        if (queryError.res?.status === 404) return false;
        return failureCount < 2;
      },
    });

  const stableCurrentQuery = useQuery<ApiLazerLookupResult, FetchError>({
    queryKey: ['stable-current', songFolder || 'auto'],
    queryFn: () => BeatmapApi.getStableCurrent(songFolder),
    enabled: true,
    refetchOnWindowFocus: false,
    refetchInterval: 1800,
    retry: false,
  });

  const beatmaps: Beatmap[] = data?.pages.flatMap((p) => p.items) ?? [];
  const firstPageLoaded = data?.pages?.[0];
  const noResults = !isFetching && !isFetchingNextPage && beatmaps.length === 0 && !error;
  const lastPage = data?.pages[data.pages.length - 1];
  const showNextPagePlaceholder =
    isFetchingNextPage || (lastPage && lastPage.items.length === 0 && lastPage.hasMore);
  const stableCurrentResult: CurrentBeatmapData | null = useMemo(() => {
    if (
      stableCurrentQuery.data &&
      stableCurrentQuery.data.status === 'folder_found' &&
      stableCurrentQuery.data.beatmap &&
      stableCurrentQuery.data.folderPath &&
      stableCurrentQuery.data.lookupRoot
    ) {
      return {
        beatmap: stableCurrentQuery.data.beatmap,
        folderPath: stableCurrentQuery.data.folderPath,
        lookupRoot: stableCurrentQuery.data.lookupRoot,
      };
    }

    return null;
  }, [stableCurrentQuery.data]);

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
  }, [debouncedSearch, songFolder]);

  const renderTopStatus = () => {
    const stableCurrentStatus = stableCurrentQuery.data?.status;
    if (stableCurrentStatus === 'ambiguous_client') {
      return (
        <Alert
          icon={<IconAlertCircle />}
          color="yellow"
          title="Ambiguous osu! client"
          mt="xs"
          variant="light"
        >
          <Text size="sm">
            {stableCurrentQuery.data?.message ??
              'Could not confidently identify osu!stable while multiple osu! clients are open.'}
          </Text>
        </Alert>
      );
    }

    if (showNextPagePlaceholder) return null;

    if (error) {
      const status = error.res?.status;
      const msg =
        status === 404
          ? debouncedSearch
            ? 'The search yielded no results.'
            : 'No mapsets could be found in the songs folder.'
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
            : 'No mapsets could be found in the songs folder.'}
        </Alert>
      );
    }

    return null;
  };

  const renderStableCurrentMap = () => {
    if (!songFolder) return null;

    return (
      <Collapse in={!!stableCurrentResult}>
        <Text size="xs" fw={500} my="sm" ml="sm" c="dimmed">
          Current mapset
        </Text>
        <CurrentBeatmapCard
          current={stableCurrentResult}
          selectedFolderPath={selectedFolderPath}
          onSelectFolderPath={setSelectedFolderPath}
        />
        <Divider my="sm" />
      </Collapse>
    );
  };

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
          <Tooltip label="Refresh beatmap search">
            <ActionIcon
              variant="default"
              onClick={() => {
                queryClient.resetQueries({
                  queryKey: ['beatmaps'],
                });
                stableCurrentQuery.refetch();
              }}
              size="36"
            >
              <IconRefresh />
            </ActionIcon>
          </Tooltip>
        </Flex>
        {!songFolder && (
          <Alert
            icon={<IconAlertCircle />}
            title="Song folder not set"
            color="yellow"
            variant="light"
          >
            <Text size="sm" mb="xs">
              Stable mode requires the osu! Songs folder.
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
        )}
        {renderTopStatus()}
      </Flex>
      {!error && !!songFolder && (
        <>
          <Divider />
          <ScrollArea
            type="auto"
            offsetScrollbars="present"
            viewportRef={scrollRef}
            className="flipped-scrollbar"
            p="xs"
            style={{ flex: '1 1 auto' }}
          >
            <Flex direction="column" gap="xs" w="100%" style={{ justifyContent: 'center' }}>
              {renderStableCurrentMap()}
              {!firstPageLoaded && <PlaceholderBeatmapCard />}
              {beatmaps.map((bm) => (
                <BeatmapCard key={bm.folder + bm.title} beatmap={bm} songFolder={songFolder} />
              ))}
              <div ref={sentinelRef} style={{ height: 1 }} />
              {showNextPagePlaceholder && <PlaceholderBeatmapCard />}
              {beatmaps.length > 0 && !hasNextPage && !isFetchingNextPage && !error && (
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
