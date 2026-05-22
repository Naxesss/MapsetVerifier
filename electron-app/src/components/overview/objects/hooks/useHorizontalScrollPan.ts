import { useCallback, useEffect, useRef, useState, type MouseEvent as ReactMouseEvent } from 'react';

export function useHorizontalScrollPan() {
  const scrollRef = useRef<HTMLDivElement | null>(null);
  const dragState = useRef({ isDragging: false, startX: 0, scrollLeft: 0 });
  const [isPanningTimeline, setIsPanningTimeline] = useState(false);

  const stopDragging = useCallback(() => {
    dragState.current.isDragging = false;
    setIsPanningTimeline(false);
  }, []);

  const handleMouseDown = (event: ReactMouseEvent<HTMLDivElement>) => {
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
  };

  const handleMouseMove = (event: ReactMouseEvent<HTMLDivElement>) => {
    const container = scrollRef.current;
    if (!container || !dragState.current.isDragging) return;

    const deltaX = event.clientX - dragState.current.startX;
    container.scrollLeft = dragState.current.scrollLeft - deltaX;
  };

  useEffect(() => {
    if (!isPanningTimeline) {
      return;
    }

    const handleWindowMouseUp = () => {
      stopDragging();
    };

    window.addEventListener('mouseup', handleWindowMouseUp);
    return () => {
      window.removeEventListener('mouseup', handleWindowMouseUp);
    };
  }, [isPanningTimeline, stopDragging]);

  return { scrollRef, isPanningTimeline, handleMouseDown, handleMouseMove, stopDragging };
}
