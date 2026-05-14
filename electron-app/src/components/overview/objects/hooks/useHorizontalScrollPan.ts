import { useCallback, useRef, useState, type MouseEvent as ReactMouseEvent } from 'react';

export function useHorizontalScrollPan() {
  const scrollRef = useRef<HTMLDivElement | null>(null);
  const dragState = useRef({ isDragging: false, startX: 0, scrollLeft: 0 });
  const [isPanningTimeline, setIsPanningTimeline] = useState(false);

  const handleMouseDown = useCallback((event: ReactMouseEvent<HTMLDivElement>) => {
    const container = scrollRef.current;
    if (!container) return;

    const target = event.target as HTMLElement | null;
    if (target?.closest('[data-stop-timeline-pan="true"]')) {
      return;
    }

    dragState.current = {
      isDragging: true,
      startX: event.clientX,
      scrollLeft: container.scrollLeft,
    };
    setIsPanningTimeline(true);
  }, []);

  const handleMouseMove = useCallback((event: ReactMouseEvent<HTMLDivElement>) => {
    const container = scrollRef.current;
    if (!container || !dragState.current.isDragging) return;

    const deltaX = event.clientX - dragState.current.startX;
    container.scrollLeft = dragState.current.scrollLeft - deltaX;
  }, []);

  /** Clears drag state; returns whether a pan drag was in progress before clear. */
  const stopDragging = useCallback((): boolean => {
    const wasDragging = dragState.current.isDragging;
    dragState.current.isDragging = false;
    setIsPanningTimeline(false);
    return wasDragging;
  }, []);

  return { scrollRef, isPanningTimeline, handleMouseDown, handleMouseMove, stopDragging };
}
