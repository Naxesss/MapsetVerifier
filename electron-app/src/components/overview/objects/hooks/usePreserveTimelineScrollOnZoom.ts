import { useLayoutEffect, useRef, type RefObject } from 'react';
import { getScrollLeftForTimestamp, getTimestampAtPlayhead } from '../timelineUtils.ts';

type UsePreserveTimelineScrollOnZoomOptions = {
  scrollRef: RefObject<HTMLDivElement | null>;
  timelineWidth: number;
  startTimeMs: number;
  endTimeMs: number;
  originX: number;
};

export function usePreserveTimelineScrollOnZoom({
  scrollRef,
  timelineWidth,
  startTimeMs,
  endTimeMs,
  originX,
}: UsePreserveTimelineScrollOnZoomOptions) {
  const prevTimelineWidthRef = useRef(timelineWidth);
  const prevOriginXRef = useRef(originX);

  // The padding before the timeline is measured after the first render, so following it here is
  // also what opens the timeline flush against the labels rather than on empty space.
  useLayoutEffect(() => {
    const container = scrollRef.current;
    const prevOriginX = prevOriginXRef.current;
    prevOriginXRef.current = originX;

    if (container) {
      container.scrollLeft += originX - prevOriginX;
    }
  }, [originX, scrollRef]);

  useLayoutEffect(() => {
    const container = scrollRef.current;
    const prevTimelineWidth = prevTimelineWidthRef.current;
    prevTimelineWidthRef.current = timelineWidth;

    if (!container || prevTimelineWidth === timelineWidth) {
      return;
    }

    const anchorViewportX = container.clientWidth / 2;

    const timestampMs = getTimestampAtPlayhead(
      container.scrollLeft,
      anchorViewportX,
      originX,
      prevTimelineWidth,
      startTimeMs,
      endTimeMs
    );

    const nextScrollLeft = getScrollLeftForTimestamp(
      timestampMs,
      anchorViewportX,
      originX,
      timelineWidth,
      startTimeMs,
      endTimeMs
    );

    const maxScrollLeft = Math.max(0, container.scrollWidth - container.clientWidth);
    container.scrollLeft = Math.max(0, Math.min(maxScrollLeft, nextScrollLeft));
  }, [endTimeMs, originX, scrollRef, startTimeMs, timelineWidth]);
}
