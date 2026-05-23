import { useLayoutEffect, useMemo, useRef, useState, type RefObject } from 'react';
import { LABEL_WIDTH } from '../constants.ts';
import {
  EMPTY_PLAYHEAD_SCROLL_PADDING,
  getPlayheadScrollPadding,
  getScrollLeftForTimestamp,
  getTimestampAtPlayhead,
  type TimelinePlayheadScrollPadding,
} from '../timelineUtils.ts';

type UsePlayheadScrollPaddingOptions = {
  timelineWidth: number;
  startTimeMs: number;
  endTimeMs: number;
};

export function usePlayheadScrollPadding(
  scrollRef: RefObject<HTMLDivElement | null>,
  playheadViewportX: number | null,
  options?: UsePlayheadScrollPaddingOptions
) {
  const [viewportWidth, setViewportWidth] = useState(0);
  const prevPaddingRef = useRef<TimelinePlayheadScrollPadding>(EMPTY_PLAYHEAD_SCROLL_PADDING);

  useLayoutEffect(() => {
    const container = scrollRef.current;
    if (!container) {
      return;
    }

    const updateViewportWidth = () => {
      setViewportWidth(container.clientWidth);
    };

    updateViewportWidth();
    const observer = new ResizeObserver(updateViewportWidth);
    observer.observe(container);

    return () => {
      observer.disconnect();
    };
  }, [scrollRef]);

  const padding = useMemo(() => {
    if (playheadViewportX === null || viewportWidth <= 0) {
      return EMPTY_PLAYHEAD_SCROLL_PADDING;
    }

    return getPlayheadScrollPadding(playheadViewportX, LABEL_WIDTH, viewportWidth);
  }, [playheadViewportX, viewportWidth]);

  useLayoutEffect(() => {
    const container = scrollRef.current;
    const prev = prevPaddingRef.current;
    prevPaddingRef.current = padding;

    if (
      playheadViewportX === null ||
      !options ||
      viewportWidth <= 0 ||
      !container ||
      (prev.padLeft === padding.padLeft && prev.padRight === padding.padRight)
    ) {
      return;
    }

    if (prev.padLeft === 0 && prev.padRight === 0) {
      return;
    }

    const timestampMs = getTimestampAtPlayhead(
      container.scrollLeft,
      playheadViewportX,
      LABEL_WIDTH,
      options.timelineWidth,
      options.startTimeMs,
      options.endTimeMs,
      prev
    );
    const nextScrollLeft = getScrollLeftForTimestamp(
      timestampMs,
      playheadViewportX,
      LABEL_WIDTH,
      options.timelineWidth,
      options.startTimeMs,
      options.endTimeMs,
      padding
    );
    const maxScrollLeft = Math.max(0, container.scrollWidth - container.clientWidth);
    container.scrollLeft = Math.max(0, Math.min(maxScrollLeft, nextScrollLeft));
  }, [
    options?.endTimeMs,
    options?.startTimeMs,
    options?.timelineWidth,
    padding,
    playheadViewportX,
    scrollRef,
    viewportWidth,
  ]);

  return padding;
}
