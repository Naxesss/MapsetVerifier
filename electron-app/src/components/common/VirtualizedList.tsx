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
  }, [items.length, updateScrollContext]);

  const virtualizer = useVirtualizer({
    count: items.length,
    estimateSize,
    getItemKey: (index) => {
      const item = items[index];
      return item ? getItemKey(item, index) : index;
    },
    getScrollElement: () => scrollElement,
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
