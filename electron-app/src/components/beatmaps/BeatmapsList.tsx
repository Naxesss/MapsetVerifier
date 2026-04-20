import {
  Alert,
  CloseButton,
  Divider,
  Flex,
  TextInput,
  Button,
  Text,
  ScrollArea,
  ActionIcon, Tooltip,
} from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconAlertCircle, IconListDetails, IconRefresh, IconSearchOff } from '@tabler/icons-react';
import { useInfiniteQuery, useQueryClient } from '@tanstack/react-query';
import { useRef, useEffect, useState } from 'react';
import BeatmapCard from './BeatmapCard';
import PlaceholderBeatmapCard from './PlaceholderBeatmapCard.tsx';
import { FetchError } from '../../client/ApiHelper.ts';
import BeatmapApi from '../../client/BeatmapApi.ts';
import { ApiBeatmapPage, Beatmap } from '../../Types.ts';

interface Props {
  songFolder: string;
}

export default function BeatmapsList({ songFolder }: Props) {
  const [search, setSearch] = useState('');
  const [debouncedSearch] = useDebouncedValue(search, 300);
  const sentinelRef = useRef<HTMLDivElement | null>(null);
  const scrollRef = useRef<HTMLDivElement | null>(null);
  const queryClient = useQueryClient();
  const stepSize = 16;

  const { data, error, isFetching, isFetchingNextPage, fetchNextPage, hasNextPage, refetch } = useInfiniteQuery<
    ApiBeatmapPage,
    FetchError
  >({
    queryKey: ['beatmaps', songFolder, debouncedSearch, stepSize],
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
        // Treat 404 as an empty terminal page so we stop requesting more
        if (err instanceof FetchError && err.res?.status === 404) {
          return { items: [], page: Number(pageParam), pageSize: stepSize, hasMore: false };
        }
        throw err; // Other errors propagate
      }
    },
    getNextPageParam: (lastPage) => (lastPage.hasMore ? lastPage.page + 1 : undefined),
    staleTime: Infinity,
    retry: (failureCount, error) => {
      // Don't retry 404s; allow one attempt
      if (error.res?.status === 404) return false;
      return failureCount < 2; // small retry for transient errors
    },
  });

  const beatmaps: Beatmap[] = data?.pages.flatMap((p) => p.items) ?? [];
  const firstPageLoaded = data?.pages?.[0];
  const noResults = !isFetching && !isFetchingNextPage && beatmaps.length === 0 && !error;
  const lastPage = data?.pages[data.pages.length - 1];

  // When a fetched page is empty but indicates more pages, immediately fetch the next.
  useEffect(() => {
    if (!lastPage) return;
    if (lastPage.items.length === 0 && lastPage.hasMore && !isFetchingNextPage) {
      fetchNextPage();
    }
  }, [lastPage, isFetchingNextPage, fetchNextPage]);

  useEffect(() => {
    const el = sentinelRef.current;
    if (!el) return;
    if (!hasNextPage) return;
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

  // Show placeholder when:
  // 1. Initial load (no first page yet)
  // 2. Fetching a next page
  // 3. Last page is empty but hasMore (auto-skip empty pages)
  const showNextPagePlaceholder =
    isFetchingNextPage || (lastPage && lastPage.items.length === 0 && lastPage.hasMore);

  const renderTopStatus = () => {
    if (showNextPagePlaceholder) {
      return null;
    }

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

  return (
    <Flex
      direction="column"
      w="100%"
      style={{
        overflow: 'hidden',
        position: 'relative',
      }}
    >
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
                })
              }}
              size="36"
            >
              <IconRefresh />
            </ActionIcon>
          </Tooltip>
        </Flex>
        {renderTopStatus()}
      </Flex>
      {!error && (
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
              {!firstPageLoaded && <PlaceholderBeatmapCard />}
              {beatmaps.map((bm) => (
                <BeatmapCard key={bm.folder + bm.title} beatmap={bm} />
              ))}
              <div ref={sentinelRef} style={{ height: 1 }} />
              {showNextPagePlaceholder && <PlaceholderBeatmapCard />}
              {beatmaps.length > 0 && !hasNextPage && !isFetchingNextPage && !error && (
                <Alert icon={<IconListDetails />} color="gray" title="No more beatmaps" variant="light">
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
    </Flex>
  );
}
