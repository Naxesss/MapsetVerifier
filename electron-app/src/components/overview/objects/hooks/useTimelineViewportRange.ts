import { useLayoutEffect, useState, type RefObject } from 'react';
import { LABEL_WIDTH } from '../constants.ts';
import { getPaddedTimelineTileSpan, getTimelineTimeFromX } from '../timelineUtils.ts';

export type TimelineViewportRange = {
  startX: number;
  endX: number;
};

const FULL_RANGE: TimelineViewportRange = { startX: -Infinity, endX: Infinity };
const MIN_OVERSCAN_PX = 512;

export function readTimelineOverscanWindowMs(
  scrollElement: HTMLElement,
  timelineWidth: number,
  startTimeMs: number,
  endTimeMs: number
): { windowStartMs: number; windowEndMs: number } {
  const viewportWidth = scrollElement.clientWidth;
  const overscan = Math.max(viewportWidth, MIN_OVERSCAN_PX);
  const localScrollLeft = scrollElement.scrollLeft - LABEL_WIDTH;
  const durationMs = Math.max(1, endTimeMs - startTimeMs);
  const clampedStartX = Math.max(0, localScrollLeft - overscan);
  const clampedEndX = Math.min(timelineWidth, localScrollLeft + viewportWidth + overscan);

  if (clampedEndX <= clampedStartX || timelineWidth <= 0) {
    return { windowStartMs: startTimeMs, windowEndMs: endTimeMs };
  }

  return {
    windowStartMs: getTimelineTimeFromX(clampedStartX, startTimeMs, durationMs, timelineWidth),
    windowEndMs: getTimelineTimeFromX(clampedEndX, startTimeMs, durationMs, timelineWidth),
  };
}

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
      const span = getPaddedTimelineTileSpan(
        timelineWidth,
        localScrollLeft - overscan,
        localScrollLeft + viewportWidth + overscan
      );
      if (!span) {
        return;
      }

      // Publish the mounted tile span, not the scroll pixel. The span only changes when a new
      // tile enters the overscan, so scrolling inside it does not re-render every row.
      setRange((prev) => {
        if (prev.startX === span.startX && prev.endX === span.endX) {
          return prev;
        }
        return span;
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
