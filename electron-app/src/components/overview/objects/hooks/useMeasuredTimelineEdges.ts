import { useLayoutEffect, useMemo, useState, type RefObject } from 'react';
import { LABEL_WIDTH } from '../constants.ts';
import type { TimelineEdgesValue } from '../context/types.ts';

/**
 * Pads the timeline by half a view on both sides, so that the centre of the view, where the cursor
 * sits, can reach its very first and last objects.
 */
export function useMeasuredTimelineEdges(
  scrollRef: RefObject<HTMLDivElement | null>
): TimelineEdgesValue {
  const [viewportWidth, setViewportWidth] = useState(0);

  // Layout effect so that the padding is there before the first paint.
  useLayoutEffect(() => {
    const scrollElement = scrollRef.current;
    if (!scrollElement) {
      return;
    }

    const measure = () => setViewportWidth(scrollElement.clientWidth);

    measure();
    const resizeObserver = new ResizeObserver(measure);
    resizeObserver.observe(scrollElement);

    return () => resizeObserver.disconnect();
  }, [scrollRef]);

  return useMemo(() => {
    const viewCentreX = Math.floor(viewportWidth / 2);

    return {
      originX: Math.max(LABEL_WIDTH, viewCentreX),
      endPaddingPx: viewportWidth - viewCentreX,
    };
  }, [viewportWidth]);
}
