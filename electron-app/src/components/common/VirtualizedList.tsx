import { Box } from '@mantine/core';
import { useVirtualizer } from '@tanstack/react-virtual';
import React from 'react';

interface VirtualizedListProps<T> {
  items: readonly T[];
  estimateSize: () => number;
  getItemKey: (item: T, index: number) => React.Key;
  renderItem: (item: T, index: number) => React.ReactNode;
  overscan?: number;
  rowGap?: string | number;
}

function VirtualizedList<T>({
  items,
  estimateSize,
  getItemKey,
  renderItem,
  overscan = 6,
  rowGap = 'var(--mantine-spacing-xs)',
}: VirtualizedListProps<T>) {
  const listRef = React.useRef<HTMLDivElement | null>(null);
  const [scrollElement, setScrollElement] = React.useState<HTMLElement | null>(null);
  const [scrollMargin, setScrollMargin] = React.useState(0);

  const updateScrollContext = React.useCallback(() => {
    const listElement = listRef.current;
    if (!listElement) return;

    const nextScrollElement = listElement.closest<HTMLElement>('.mantine-ScrollArea-viewport');
    setScrollElement(nextScrollElement);

    if (!nextScrollElement) {
      setScrollMargin(0);
      return;
    }

    const listRect = listElement.getBoundingClientRect();
    const scrollRect = nextScrollElement.getBoundingClientRect();
    setScrollMargin(listRect.top - scrollRect.top + nextScrollElement.scrollTop);
  }, []);

  React.useLayoutEffect(() => {
    updateScrollContext();

    // Content above the list (e.g. another accordion section collapsing) can move
    // the list without changing our item count, so keep the scroll margin in sync
    // with any resize of the scroll area's content.
    const scrollContent = listRef.current?.closest<HTMLElement>(
      '.mantine-ScrollArea-viewport'
    )?.firstElementChild;
    if (!scrollContent || typeof ResizeObserver === 'undefined') return;

    const observer = new ResizeObserver(() => updateScrollContext());
    observer.observe(scrollContent);
    return () => observer.disconnect();
  }, [items.length, updateScrollContext]);

  const virtualizer = useVirtualizer({
    count: items.length,
    estimateSize,
    getItemKey: (index) => {
      const item = items[index];
      return item ? getItemKey(item, index) : index;
    },
    getScrollElement: () => scrollElement,
    // Rows inside a closed or animating Collapse (accordion panel) measure as 0.
    // Accepting 0 makes the virtualizer think every row fits on screen, mounting
    // the entire list in one commit and exceeding React's update depth.
    measureElement: (element) => {
      const height = element.getBoundingClientRect().height;
      return height > 0 ? height : estimateSize();
    },
    overscan,
    scrollMargin,
  });

  return (
    <Box
      ref={listRef}
      style={{
        height: `${virtualizer.getTotalSize()}px`,
        position: 'relative',
        width: '100%',
      }}
    >
      {virtualizer.getVirtualItems().map((virtualItem) => {
        const item = items[virtualItem.index];
        if (!item) return null;

        return (
          <Box
            key={virtualItem.key}
            data-index={virtualItem.index}
            ref={virtualizer.measureElement}
            style={{
              left: 0,
              paddingBottom: rowGap,
              position: 'absolute',
              top: 0,
              transform: `translateY(${virtualItem.start - scrollMargin}px)`,
              width: '100%',
            }}
          >
            {renderItem(item, virtualItem.index)}
          </Box>
        );
      })}
    </Box>
  );
}

export default VirtualizedList;
