import { useCallback, useEffect, type RefObject } from 'react';
import { LABEL_WIDTH } from '../constants.ts';
import {
  getAdjacentTimingSnapTick,
  getScrollLeftForTimestamp,
  getTimestampAtPlayhead,
} from '../timelineUtils.ts';

type UseTimelineWheelSeekOptions = {
  scrollRef: RefObject<HTMLDivElement | null>;
  timelineWidth: number;
  startTimeMs: number;
  endTimeMs: number;
  snapTicks: number[];
  enabled?: boolean;
};

const MAX_SAME_PIXEL_STEPS = 512;

export function useTimelineWheelSeek({
  scrollRef,
  timelineWidth,
  startTimeMs,
  endTimeMs,
  snapTicks,
  enabled = true,
}: UseTimelineWheelSeekOptions) {
  const seekByDirection = useCallback(
    (direction: 1 | -1): boolean => {
      const scrollElement = scrollRef.current;
      if (!scrollElement || scrollElement.clientWidth <= 0 || snapTicks.length === 0) {
        return false;
      }

      const viewportWidth = scrollElement.clientWidth;
      const anchorViewportX = viewportWidth / 2;

      const currentTimestamp = getTimestampAtPlayhead(
        scrollElement.scrollLeft,
        anchorViewportX,
        LABEL_WIDTH,
        timelineWidth,
        startTimeMs,
        endTimeMs
      );

      const startScrollLeft = scrollElement.scrollLeft;
      const maxScrollLeft = Math.max(0, scrollElement.scrollWidth - scrollElement.clientWidth);

      let candidateTimestamp = currentTimestamp;

      for (let step = 0; step < MAX_SAME_PIXEL_STEPS; step += 1) {
        const nextTimestamp = getAdjacentTimingSnapTick(
          snapTicks,
          candidateTimestamp,
          direction,
          startTimeMs,
          endTimeMs
        );

        if (nextTimestamp === candidateTimestamp) {
          break;
        }

        candidateTimestamp = nextTimestamp;

        const nextScrollLeft = getScrollLeftForTimestamp(
          candidateTimestamp,
          anchorViewportX,
          LABEL_WIDTH,
          timelineWidth,
          startTimeMs,
          endTimeMs
        );
        const clampedScrollLeft = Math.max(0, Math.min(maxScrollLeft, nextScrollLeft));

        if (Math.abs(clampedScrollLeft - startScrollLeft) >= 1) {
          scrollElement.scrollLeft = clampedScrollLeft;
          return true;
        }

        if (
          (direction === 1 && candidateTimestamp >= endTimeMs) ||
          (direction === -1 && candidateTimestamp <= startTimeMs)
        ) {
          break;
        }
      }

      return false;
    },
    [endTimeMs, scrollRef, snapTicks, startTimeMs, timelineWidth]
  );

  useEffect(() => {
    if (!enabled) {
      return;
    }

    const scrollElement = scrollRef.current;
    if (!scrollElement) {
      return;
    }

    const handleWheel = (event: WheelEvent) => {
      const target = event.target;
      if (!(target instanceof HTMLElement)) {
        return;
      }

      if (target.closest('[data-stop-timeline-pan="true"]')) {
        return;
      }

      if (target.closest('[data-timeline-wheel-ignore="true"]')) {
        return;
      }

      const useHorizontalDelta = Math.abs(event.deltaX) > Math.abs(event.deltaY);
      if (!event.shiftKey && !useHorizontalDelta) {
        return;
      }

      const delta = event.shiftKey ? event.deltaY : -event.deltaX;
      if (delta === 0) {
        return;
      }

      const didSeek = seekByDirection(delta > 0 ? 1 : -1);
      if (didSeek) {
        event.preventDefault();
      }
    };

    scrollElement.addEventListener('wheel', handleWheel, { passive: false });

    return () => {
      scrollElement.removeEventListener('wheel', handleWheel);
    };
  }, [enabled, scrollRef, seekByDirection]);
}
