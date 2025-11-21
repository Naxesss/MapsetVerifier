import {useEffect, useState, useRef} from "react";
import BeatmapCard from "./BeatmapCard";
import "./Beatmaps.scss";
import {Alert, CloseButton, Container, Divider, Flex, Group, Input, Loader, Text, Button} from "@mantine/core";
import PlaceholderBeatmapCards from "./PlaceholderBeatmapCards.tsx";
import { useDebouncedValue } from "@mantine/hooks";
import {Beatmap} from "../../Types.ts";

interface Props {
    songFolder: string; // Optional: if provided, will be sent to API; if blank API will auto-detect
}

export default function BeatmapsList({ songFolder }: Props) {
  const [beatmaps, setBeatmaps] = useState<Beatmap[]>([]);
  const [page, setPage] = useState(0);
  const [hasMore, setHasMore] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [debouncedSearch] = useDebouncedValue(search, 300); // 300ms debounce
  const [totalCount, setTotalCount] = useState<number | null>(null);
  const stepSize = 16;
  const sentinelRef = useRef<HTMLDivElement | null>(null);
  const scrollRef = useRef<HTMLDivElement | null>(null); // new ref to scroll container

  async function loadBeatmaps(reset: boolean) {
    setLoading(true);
    setError(null);
    if (reset) {
      setBeatmaps([]);
      setPage(0);
    }
    try {
      const currentPage = reset ? 0 : page;
      const params = new URLSearchParams();
      if (songFolder) params.append("songsFolder", songFolder);
      if (debouncedSearch) params.append("search", debouncedSearch);
      params.append("page", currentPage.toString());
      params.append("pageSize", stepSize.toString());
      const url = `http://localhost:5005/beatmaps?${params.toString()}`;
      console.log("[Beatmaps] Fetching:", url);
      const res = await fetch(url);
      if (!res.ok) {
        // Handle 404 differently depending on whether it's the first page or a subsequent page
        if (res.status === 404) {
          if (currentPage === 0) {
            const errJson = await res.json().catch(() => ({}));
            const message = errJson.Message || errJson.message || "No mapsets could be found.";
            setError(message);
            setHasMore(false);
          } else {
            // Reached end of list: no more pages
            setHasMore(false);
          }
          setLoading(false);
          return;
        }
        const errJson = await res.json().catch(() => ({}));
        const message = errJson.Message || errJson.message || `Failed to load beatmaps (HTTP ${res.status})`;
        setError(message);
        setLoading(false);
        return;
      }
      const data = await res.json();
      const items: Beatmap[] = data.items || data.Items || [];
      const pageIndex: number = data.page ?? data.Page ?? currentPage;
      const hasMoreFlag: boolean = data.hasMore ?? data.HasMore ?? false;
      const total: number | undefined = data.totalCount ?? data.TotalCount;
      if (typeof total === 'number') setTotalCount(total);
      setHasMore(hasMoreFlag);
      setPage(pageIndex);
      setBeatmaps(prev => reset ? items : [...prev, ...items]);
      if (items.length === 0 && pageIndex === 0) {
        setError(debouncedSearch ? "The search yielded no results." : "No mapsets could be found in this folder.");
      }
    } catch (e) {
      console.error("[Beatmaps] Failed to load beatmaps:", (e as Error).message);
      setError("Failed to load beatmaps: " + (e as Error).message);
    }
    setLoading(false);
  }

  useEffect(() => {
    // reset pagination when folder or search changes
    loadBeatmaps(true);
  }, [songFolder, debouncedSearch]);

  useEffect(() => {
    if (page !== 0) {
      loadBeatmaps(false);
    }
  }, [page]);

  useEffect(() => {
    const sentinel = sentinelRef.current;
    if (!sentinel) return;
    if (!hasMore) return; // Do not observe when list end reached
    const observer = new IntersectionObserver(entries => {
      const entry = entries[0];
      if (entry.isIntersecting && hasMore && !loading) {
          setPage(p => p + 1);
      }
    }, { root: null, rootMargin: '200px', threshold: 0.1 });
    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [hasMore, loading]);

  return (
    <Container className="beatmaps-container" p="unset">
      <Flex direction="column" gap="sm" p="sm">
        <Input
          type="text"
          placeholder="Search beatmaps..."
          value={search}
          onChange={e => {
            const value = e.target.value;
            // If search is being cleared, scroll to top first
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
        {totalCount !== null && (
          <Text size="sm" c="dimmed" mt="xs">
            Loaded {beatmaps.length} of {totalCount}
          </Text>
        )}
        {error && (
          <Alert color="red" title="Error" mt="xs">
            {error}
          </Alert>
        )}
      </Flex>
      <Divider />
      <Group className="beatmaps-scroll" ref={scrollRef} p="sm">
        <Flex direction="column" gap="xs" w="100%" style={{ justifyContent: "center" }}>
          {loading && page === 0 && <PlaceholderBeatmapCards />}
          {beatmaps.map(bm => (
            <BeatmapCard key={bm.folder + bm.title} beatmap={bm} />
          ))}
          <div ref={sentinelRef} style={{ height: 1 }} />
          {loading && page > 0 && (
            <Flex justify="center" my="sm">
              <Loader size="sm" />
            </Flex>
          )}
          {beatmaps.length > 0 && !hasMore && !loading && !error && (
            <Alert color="gray" title="No more beatmaps" variant="light">
              You have reached the last available beatmap.
              <Button size="xs" mt="xs" variant="default" onClick={() => scrollRef.current?.scrollTo({ top: 0, behavior: 'smooth' })}>
                Back to top
              </Button>
            </Alert>
          )}
        </Flex>
      </Group>
    </Container>
  );
}
