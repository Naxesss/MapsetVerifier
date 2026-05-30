import { useLayoutEffect, useRef, type RefObject } from 'react';
import { LABEL_WIDTH } from '../constants.ts';
import { getScrollLeftForTimestamp, getTimestampAtPlayhead } from '../timelineUtils.ts';

type UsePreserveTimelineScrollOnZoomOptions = {
  scrollRef: RefObject<HTMLDivElement | null>;
  timelineWidth: number;
  startTimeMs: number;
  endTimeMs: number;
};

export function usePreserveTimelineScrollOnZoom({
  scrollRef,
  timelineWidth,
  startTimeMs,
  endTimeMs,
}: UsePreserveTimelineScrollOnZoomOptions) {
  const prevTimelineWidthRef = useRef(timelineWidth);

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
      LABEL_WIDTH,
      prevTimelineWidth,
      startTimeMs,
      endTimeMs
    );

    const nextScrollLeft = getScrollLeftForTimestamp(
      timestampMs,
      anchorViewportX,
      LABEL_WIDTH,
      timelineWidth,
      startTimeMs,
      endTimeMs
    );

    const maxScrollLeft = Math.max(0, container.scrollWidth - container.clientWidth);
    container.scrollLeft = Math.max(0, Math.min(maxScrollLeft, nextScrollLeft));
  }, [endTimeMs, scrollRef, startTimeMs, timelineWidth]);
}
