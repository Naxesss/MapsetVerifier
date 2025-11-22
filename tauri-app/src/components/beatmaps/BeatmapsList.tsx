import { useRef, useEffect, useState } from "react";
import { Alert, CloseButton, Container, Divider, Flex, Group, Input, Loader, Button, Text } from "@mantine/core";
import { useDebouncedValue } from "@mantine/hooks";
import { useInfiniteQuery } from "@tanstack/react-query";
import BeatmapCard from "./BeatmapCard";
import PlaceholderBeatmapCards from "./PlaceholderBeatmapCards.tsx";
import BeatmapApi from "../../client/BeatmapApi.ts";
import { ApiBeatmapPage, Beatmap } from "../../Types.ts";
import { FetchError } from "../../client/ApiHelper.ts";
import "./Beatmaps.scss";

interface Props {
  songFolder: string;
}

export default function BeatmapsList({ songFolder }: Props) {
  const [search, setSearch] = useState("");
  const [debouncedSearch] = useDebouncedValue(search, 300);
  const sentinelRef = useRef<HTMLDivElement | null>(null);
  const scrollRef = useRef<HTMLDivElement | null>(null);
  const stepSize = 16;

  const {
    data,
    error,
    isLoading,
    isFetchingNextPage,
    fetchNextPage,
    hasNextPage
  } = useInfiniteQuery<ApiBeatmapPage, FetchError>({
    queryKey: ["beatmaps", songFolder, debouncedSearch, stepSize],
    initialPageParam: 0,
    queryFn: async ({ pageParam }) => {
      const params = new URLSearchParams();
      if (songFolder) params.append("songsFolder", songFolder);
      if (debouncedSearch) params.append("search", debouncedSearch);
      params.append("page", String(pageParam));
      params.append("pageSize", stepSize.toString());
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
    }
  });

  const beatmaps: Beatmap[] = data?.pages.flatMap(p => p.items) ?? [];
  const firstPageLoaded = data?.pages?.[0];
  const noResults = !isLoading && beatmaps.length === 0 && !error;

  useEffect(() => {
    const el = sentinelRef.current;
    if (!el) return;
    if (!hasNextPage) return;
    const observer = new IntersectionObserver(entries => {
      if (entries[0].isIntersecting && hasNextPage && !isFetchingNextPage) {
        fetchNextPage();
      }
    }, { root: null, rootMargin: "200px", threshold: 0.1 });
    observer.observe(el);
    return () => observer.disconnect();
  }, [hasNextPage, isFetchingNextPage, fetchNextPage]);

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: 0, behavior: "auto" });
  }, [debouncedSearch, songFolder]);

  const renderTopStatus = () => {
    if (error) {
      const status = error.res?.status;
      const msg = status === 404
        ? (debouncedSearch ? "The search yielded no results." : "No mapsets could be found in the songs folder.")
        : (error.message || "Failed to load beatmaps.");
      return <Alert color="red" title="Error" mt="xs">{msg}</Alert>;
    }
    if (noResults) {
      return (
        <Alert color="gray" title="No results" mt="xs" variant="light">
          {debouncedSearch ? "The search yielded no results." : "No mapsets could be found in the songs folder."}
        </Alert>
      );
    }
    return null;
  };

  return (
    <Container className="beatmaps-container" p="unset">
      <Flex direction="column" gap="sm" p="sm">
        <Input
          type="text"
          placeholder="Search beatmaps..."
          value={search}
          onChange={e => {
            const value = e.target.value;
            if (value === "" && search !== "") {
              scrollRef.current?.scrollTo({ top: 0, behavior: "auto" });
            }
            setSearch(value);
          }}
          rightSectionPointerEvents="all"
          rightSection={
            <CloseButton
              aria-label="Clear input"
              onClick={() => {
                if (search !== "") {
                  scrollRef.current?.scrollTo({ top: 0, behavior: "auto" });
                }
                setSearch("");
              }}
              style={{ display: search ? undefined : "none" }}
            />
          }
        />
        {renderTopStatus()}
      </Flex>
      <Divider />
      <Group className="beatmaps-scroll" ref={scrollRef} p="sm">
        <Flex direction="column" gap="xs" w="100%" style={{ justifyContent: "center" }}>
          {isLoading && !firstPageLoaded && <PlaceholderBeatmapCards />}
          {beatmaps.map(bm => (
            <BeatmapCard key={bm.folder + bm.title} beatmap={bm} />
          ))}
          <div ref={sentinelRef} style={{ height: 1 }} />
          {isFetchingNextPage && (
            <Flex justify="center" my="sm">
              <Loader size="sm" />
            </Flex>
          )}
          {beatmaps.length > 0 && !hasNextPage && !isFetchingNextPage && !error && (
            <Alert color="gray" title="No more beatmaps" variant="light">
              You have reached the last available beatmap.
              <Button
                size="xs"
                mt="xs"
                variant="default"
                onClick={() => scrollRef.current?.scrollTo({ top: 0, behavior: "smooth" })}
              >
                Back to top
              </Button>
            </Alert>
          )}
          {error && beatmaps.length > 0 && (
            <Text c="red" size="sm" ta="center" my="xs">
              {error.message || "An error occurred."}
            </Text>
          )}
        </Flex>
      </Group>
    </Container>
  );
}
