import { useLayoutEffect, useState, type RefObject } from 'react';
import { LABEL_WIDTH } from '../constants.ts';

export type TimelineViewportRange = {
  startX: number;
  endX: number;
};

const FULL_RANGE: TimelineViewportRange = { startX: -Infinity, endX: Infinity };
const MIN_OVERSCAN_PX = 512;

export function useTimelineViewportRange(
  scrollRef: RefObject<HTMLDivElement | null>,
  timelineWidth: number
): TimelineViewportRange {
  const [range, setRange] = useState<TimelineViewportRange>(FULL_RANGE);

  // Layout effect (not a plain effect) so that when `timelineWidth` changes (zoom), this runs
  // synchronously before paint — and, since layout effects fire child-before-parent, after any
  // scrollLeft correction a child (e.g. usePreserveTimelineScrollOnZoom) makes in the same commit.
  // Skipping this would leave stale pixel bounds filtering tiles at the new scale for one frame,
  // which briefly renders nothing (tiles computed with the old window never overlap the new one).
  useLayoutEffect(() => {
    const scrollElement = scrollRef.current;
    if (!scrollElement) {
      return;
    }

    let frame = 0;

    const updateRange = () => {
      frame = 0;
      const viewportWidth = scrollElement.clientWidth;
      const overscan = Math.max(viewportWidth, MIN_OVERSCAN_PX);
      const localScrollLeft = scrollElement.scrollLeft - LABEL_WIDTH;

      setRange((prev) => {
        const startX = localScrollLeft - overscan;
        const endX = localScrollLeft + viewportWidth + overscan;
        if (prev.startX === startX && prev.endX === endX) {
          return prev;
        }
        return { startX, endX };
      });
    };

    const handleScroll = () => {
      if (frame) {
        return;
      }
      frame = requestAnimationFrame(updateRange);
    };

    updateRange();

    scrollElement.addEventListener('scroll', handleScroll, { passive: true });
    const resizeObserver = new ResizeObserver(handleScroll);
    resizeObserver.observe(scrollElement);

    return () => {
      if (frame) {
        cancelAnimationFrame(frame);
      }
      scrollElement.removeEventListener('scroll', handleScroll);
      resizeObserver.disconnect();
    };
  }, [scrollRef, timelineWidth]);

  return range;
}
