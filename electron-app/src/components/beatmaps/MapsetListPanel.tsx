import { Collapse, Divider, Flex, ScrollArea } from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { useInfiniteQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useRef, useState, type ReactNode } from 'react';
import {
  MapsetListEmpty,
  MapsetListEnd,
  MapsetListError,
  MapsetListToolbar,
} from './MapsetListParts.tsx';
import PlaceholderBeatmapCard from './PlaceholderBeatmapCard.tsx';
import { FetchError } from '../../client/ApiHelper.ts';
import { useSettings } from '../../context/SettingsContext.tsx';
import { ApiBeatmapPage, Beatmap } from '../../Types.ts';
import { MicroLabel } from '../common/Headings.tsx';

const PAGE_SIZE = 16;

export interface MapsetPageRequest {
  page: number;
  pageSize: number;
  search: string;
  /** Set while only bookmarked mapsets are shown. */
  bookmarkedFolders: string[] | null;
}

interface MapsetListPanelProps {
  /** Query key of this library's list, e.g. `['beatmaps', songFolder]`; search and paging are added. */
  listKey: readonly unknown[];
  fetchPage: (request: MapsetPageRequest) => Promise<ApiBeatmapPage>;
  listOptions?: { enabled?: boolean; staleTime?: number; refetchOnWindowFocus?: boolean };
  /** Where the mapsets come from, e.g. "osu! Songs folder". */
  libraryName: string;
  /** Shown when the list fails to load without a message of its own. */
  loadErrorMessage: string;
  /** Library-specific notices under the search row, e.g. a missing folder. */
  notices?: ReactNode;
  /** Hides the list, e.g. while the library's folder isn't set. */
  listHidden?: boolean;
  /** Whether a mapset is open in osu!; the current mapset block shows while it is. */
  hasCurrent: boolean;
  /** The card of the mapset open in osu!. */
  currentCard: ReactNode;
  renderCard: (beatmap: Beatmap, index: number) => ReactNode;
  /** Refreshes what the panel doesn't own, such as the current mapset. */
  onRefresh: () => void;
}

/**
 * A library's mapset list in the sidebar: search and bookmark filter, the mapset open in osu!, and
 * the mapsets, loaded a page at a time as the list scrolls. The stable and lazer panels differ only
 * in how they fetch and in what their cards do.
 */
export default function MapsetListPanel({
  listKey,
  fetchPage,
  listOptions,
  libraryName,
  loadErrorMessage,
  notices,
  listHidden = false,
  hasCurrent,
  currentCard,
  renderCard,
  onRefresh,
}: MapsetListPanelProps) {
  const { settings } = useSettings();
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [debouncedSearch] = useDebouncedValue(search, 300);
  const [bookmarkedOnly, setBookmarkedOnly] = useState(false);
  const sentinelRef = useRef<HTMLDivElement | null>(null);
  const scrollRef = useRef<HTMLDivElement | null>(null);

  const bookmarkedFolders = settings.bookmarkedFolders;
  const filterByBookmarks = bookmarkedOnly && settings.bookmarksEnabled;
  const hasNoBookmarks = filterByBookmarks && bookmarkedFolders.length === 0;

  const { data, error, isFetching, isFetchingNextPage, fetchNextPage, hasNextPage, refetch } =
    useInfiniteQuery<ApiBeatmapPage, FetchError>({
      queryKey: [
        ...listKey,
        debouncedSearch,
        PAGE_SIZE,
        filterByBookmarks ? bookmarkedFolders : null,
      ],
      enabled: (listOptions?.enabled ?? true) && !hasNoBookmarks,
      initialPageParam: 0,
      queryFn: async ({ pageParam }) => {
        const page = Number(pageParam);
        try {
          return await fetchPage({
            page,
            pageSize: PAGE_SIZE,
            search: debouncedSearch,
            bookmarkedFolders: filterByBookmarks ? bookmarkedFolders : null,
          });
        } catch (err) {
          // The backend answers 404 for an empty page.
          if (err instanceof FetchError && err.res?.status === 404) {
            return { items: [], page, pageSize: PAGE_SIZE, hasMore: false };
          }
          throw err;
        }
      },
      getNextPageParam: (lastPage) => (lastPage.hasMore ? lastPage.page + 1 : undefined),
      staleTime: listOptions?.staleTime,
      refetchOnWindowFocus: listOptions?.refetchOnWindowFocus,
      retry: (failureCount, queryError) => {
        if (queryError.res?.status === 404) return false;
        return failureCount < 2;
      },
    });

  const beatmaps: Beatmap[] = data?.pages.flatMap((p) => p.items) ?? [];
  const firstPageLoaded = data?.pages?.[0];
  const noResults = !isFetching && !isFetchingNextPage && beatmaps.length === 0 && !error;
  const lastPage = data?.pages[data.pages.length - 1];
  const showNextPagePlaceholder =
    isFetchingNextPage || (lastPage && lastPage.items.length === 0 && lastPage.hasMore);

  // A page that came back empty but says more follow is skipped straight away.
  useEffect(() => {
    if (!lastPage) return;
    if (lastPage.items.length === 0 && lastPage.hasMore && !isFetchingNextPage) {
      void fetchNextPage();
    }
  }, [lastPage, isFetchingNextPage, fetchNextPage]);

  // Loads the next page as the end of the list scrolls into view.
  useEffect(() => {
    const el = sentinelRef.current;
    if (!el || !hasNextPage) return;
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasNextPage && !isFetchingNextPage) {
          void fetchNextPage();
        }
      },
      { root: null, rootMargin: '200px', threshold: 0.1 }
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, [hasNextPage, isFetchingNextPage, fetchNextPage]);

  const listKeyToken = JSON.stringify(listKey);
  useEffect(() => {
    scrollRef.current?.scrollTo({ top: 0, behavior: 'auto' });
  }, [debouncedSearch, listKeyToken]);

  const renderStatus = () => {
    if (showNextPagePlaceholder) return null;

    if (error) {
      return (
        <MapsetListError
          message={error.message || loadErrorMessage}
          onRetry={() => void refetch()}
        />
      );
    }

    if (hasNoBookmarks || noResults) {
      return (
        <MapsetListEmpty
          search={debouncedSearch}
          bookmarkedOnly={filterByBookmarks}
          noBookmarks={hasNoBookmarks}
          libraryName={libraryName}
        />
      );
    }

    return null;
  };

  return (
    <>
      <Flex direction="column" gap="sm" p="xs">
        <MapsetListToolbar
          search={search}
          onSearchChange={(value) => {
            if (value === '' && search !== '') {
              scrollRef.current?.scrollTo({ top: 0, behavior: 'auto' });
            }
            setSearch(value);
          }}
          bookmarksEnabled={settings.bookmarksEnabled}
          bookmarkedOnly={bookmarkedOnly}
          onToggleBookmarkedOnly={() => setBookmarkedOnly((prev) => !prev)}
          onRefresh={() => {
            void queryClient.resetQueries({ queryKey: [listKey[0]] });
            onRefresh();
          }}
        />
        {notices}
        {renderStatus()}
      </Flex>
      {!error && !listHidden && !hasNoBookmarks && (
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
              <Collapse in={hasCurrent}>
                <MicroLabel my="sm" ml="sm">
                  Current mapset
                </MicroLabel>
                {currentCard}
                <Divider my="sm" />
              </Collapse>
              {!firstPageLoaded &&
                Array.from({ length: 6 }).map((_, i) => <PlaceholderBeatmapCard key={i} />)}
              {beatmaps.map((beatmap, index) => renderCard(beatmap, index))}
              <div ref={sentinelRef} style={{ height: 1 }} />
              {showNextPagePlaceholder && <PlaceholderBeatmapCard />}
              {beatmaps.length > 0 && !hasNextPage && !isFetchingNextPage && !filterByBookmarks && (
                <MapsetListEnd
                  onBackToTop={() => scrollRef.current?.scrollTo({ top: 0, behavior: 'smooth' })}
                />
              )}
            </Flex>
          </ScrollArea>
        </>
      )}
    </>
  );
}
