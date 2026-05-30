import { useLayoutEffect, useRef, type RefObject } from 'react';
import { LABEL_WIDTH } from '../constants.ts';
import {
  getPlayheadScrollPadding,
  getScrollLeftForTimestamp,
  getTimestampAtPlayhead,
} from '../timelineUtils.ts';

type UsePreserveTimelineScrollOnZoomOptions = {
  scrollRef: RefObject<HTMLDivElement | null>;
  timelineWidth: number;
  startTimeMs: number;
  endTimeMs: number;
  /** When set (e.g. hitsound playhead), zoom keeps this viewport X pinned to the same timestamp. */
  anchorViewportX?: number | null;
};

export function usePreserveTimelineScrollOnZoom({
  scrollRef,
  timelineWidth,
  startTimeMs,
  endTimeMs,
  anchorViewportX = null,
}: UsePreserveTimelineScrollOnZoomOptions) {
  const prevTimelineWidthRef = useRef(timelineWidth);
  const prevAnchorViewportXRef = useRef(anchorViewportX);

  useLayoutEffect(() => {
    const container = scrollRef.current;
    const prevTimelineWidth = prevTimelineWidthRef.current;
    const prevAnchorViewportX = prevAnchorViewportXRef.current;
    prevTimelineWidthRef.current = timelineWidth;
    prevAnchorViewportXRef.current = anchorViewportX;

    if (!container || prevTimelineWidth === timelineWidth) {
      return;
    }

    const readAnchorX = prevAnchorViewportX ?? anchorViewportX ?? container.clientWidth / 2;
    const writeAnchorX = anchorViewportX ?? prevAnchorViewportX ?? container.clientWidth / 2;
    const readPadding =
      prevAnchorViewportX != null
        ? getPlayheadScrollPadding(prevAnchorViewportX, LABEL_WIDTH, container.clientWidth)
        : undefined;
    const writePadding =
      anchorViewportX != null
        ? getPlayheadScrollPadding(anchorViewportX, LABEL_WIDTH, container.clientWidth)
        : undefined;

    const timestampMs = getTimestampAtPlayhead(
      container.scrollLeft,
      readAnchorX,
      LABEL_WIDTH,
      prevTimelineWidth,
      startTimeMs,
      endTimeMs,
      readPadding
    );

    const nextScrollLeft = getScrollLeftForTimestamp(
      timestampMs,
      writeAnchorX,
      LABEL_WIDTH,
      timelineWidth,
      startTimeMs,
      endTimeMs,
      writePadding
    );

    const maxScrollLeft = Math.max(0, container.scrollWidth - container.clientWidth);
    container.scrollLeft = Math.max(0, Math.min(maxScrollLeft, nextScrollLeft));
  }, [anchorViewportX, endTimeMs, scrollRef, startTimeMs, timelineWidth]);
}
