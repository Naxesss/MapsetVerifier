import { Flex, SimpleGrid, Skeleton, Stack } from '@mantine/core';

/*
 * One way to show loading across the app: content-shaped skeletons where content is on its way
 * (lists, card pages, text), and a small Loader only for inline waits such as a button or a
 * control that is applying a change. A refresh of content that is already shown keeps it on
 * screen; the refresh toast says it is updating.
 */

/** Rows of a list, e.g. documentation checks or ranking criteria rules. */
export function ListSkeleton({ rows = 6 }: { rows?: number }) {
  return (
    <Stack gap="xs" w="100%" aria-busy aria-label="Loading">
      {Array.from({ length: rows }).map((_, i) => (
        <Skeleton key={i} height={56} radius="md" />
      ))}
    </Stack>
  );
}

/** A page of cards, e.g. an Overview tab or the Snapshots history, padded like that content. */
export function CardsSkeleton() {
  return (
    <Stack gap="md" p="md" aria-busy aria-label="Loading">
      <SimpleGrid cols={{ base: 1, lg: 2 }} spacing="md">
        <Skeleton height={200} radius="md" />
        <Skeleton height={200} radius="md" />
      </SimpleGrid>
      <Skeleton height={280} radius="md" />
    </Stack>
  );
}

/**
 * The selected-difficulty row over a narrow list next to a wide panel, e.g. the snapshot history
 * and its changes, padded like that page's content.
 */
export function HistorySkeleton() {
  return (
    <Stack gap="sm" px="md" pt="sm" pb="md" aria-busy aria-label="Loading">
      <Skeleton height={36} radius="md" />
      <Flex gap="md" direction={{ base: 'column', md: 'row' }}>
        <Skeleton
          height={240}
          radius="md"
          w={{ base: '100%', md: 280 }}
          style={{ flexShrink: 0 }}
        />
        <Skeleton height={320} radius="md" style={{ flex: 1 }} />
      </Flex>
    </Stack>
  );
}

/** Lines of text, e.g. documentation or rule text in a drawer or modal. */
export function TextSkeleton({ lines = 3 }: { lines?: number }) {
  const widths = ['100%', '92%', '76%', '84%', '60%'];

  return (
    <Stack gap="xs" aria-busy aria-label="Loading">
      {Array.from({ length: lines }).map((_, i) => (
        <Skeleton key={i} height={12} radius="xl" width={widths[i % widths.length]} />
      ))}
    </Stack>
  );
}

/** One chart or media area inside a card, at the height of what replaces it. */
export function BlockSkeleton({ height }: { height: number }) {
  return <Skeleton height={height} radius="md" aria-busy aria-label="Loading" />;
}

/**
 * Stand-in for a whole page for the frame or two it takes to render, so switching pages is
 * instant: a toolbar row and a list. Mapset pages render it inside their MapsetFrame, under the
 * mapset title, padded like their header and content.
 */
export function PageSkeleton({ variant }: { variant: 'mapset' | 'list' }) {
  if (variant === 'mapset') {
    return (
      <Stack gap="md" pt="sm" px="md" pb="md" aria-busy aria-label="Loading">
        <Skeleton height={36} radius="md" />
        <ListSkeleton rows={4} />
      </Stack>
    );
  }

  return (
    <Stack gap="sm" aria-busy aria-label="Loading">
      <Skeleton height={36} radius="md" />
      <ListSkeleton />
    </Stack>
  );
}
