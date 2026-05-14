import { createContext, memo, useEffect, useState, type ReactNode, type RefObject } from 'react';
import { TIMELINE_TILE_OVERSCAN_PX } from '../constants.ts';
import { getTimelineCanvasTiles, getVisibleTimelineCanvasTileIndices } from '../timelineUtils.ts';

/** Inclusive index range into canvas tiles intersecting viewport + horizontal overscan. */
export type TimelineTileVisibilityRange = Readonly<{ start: number; end: number }>;

export const TimelineTileVisibilityContext = createContext<TimelineTileVisibilityRange | null>(
  null
);

function fullTileRange(width: number): TimelineTileVisibilityRange {
  const count = getTimelineCanvasTiles(width).length;
  if (count === 0) return { start: 0, end: -1 };
  return { start: 0, end: count - 1 };
}

function useTimelineTileVisibilityRange(
  scrollRef: RefObject<HTMLDivElement | null>,
  timelineWidth: number
): TimelineTileVisibilityRange {
  const [range, setRange] = useState<TimelineTileVisibilityRange>(() =>
    fullTileRange(timelineWidth)
  );

  useEffect(() => {
    setRange(fullTileRange(timelineWidth));

    let raf = 0;
    let lastKey = '';

    const commit = () => {
      raf = 0;
      const el = scrollRef.current;
      if (!el || timelineWidth <= 0) return;

      const { start, end } = getVisibleTimelineCanvasTileIndices(
        el.scrollLeft,
        el.clientWidth,
        timelineWidth,
        TIMELINE_TILE_OVERSCAN_PX
      );
      const key = `${timelineWidth}:${start}:${end}`;
      if (key !== lastKey) {
        lastKey = key;
        setRange({ start, end });
      }
    };

    const schedule = () => {
      if (raf !== 0) cancelAnimationFrame(raf);
      raf = requestAnimationFrame(commit);
    };

    schedule();

    const el = scrollRef.current;
    el?.addEventListener('scroll', schedule, { passive: true });

    const ro = new ResizeObserver(schedule);
    if (el) ro.observe(el);

    return () => {
      cancelAnimationFrame(raf);
      el?.removeEventListener('scroll', schedule);
      ro.disconnect();
    };
  }, [scrollRef, timelineWidth]);

  return range;
}

export interface TimelineTileVisibilityBoundaryProps {
  scrollRef: RefObject<HTMLDivElement | null>;
  timelineWidth: number;
  children: ReactNode;
}

/** Subscribes to horizontal scroll/size and exposes which canvas tiles should mount heavy draw calls. */
export const TimelineTileVisibilityBoundary = memo(function TimelineTileVisibilityBoundary({
  scrollRef,
  timelineWidth,
  children,
}: TimelineTileVisibilityBoundaryProps) {
  const range = useTimelineTileVisibilityRange(scrollRef, timelineWidth);

  return (
    <TimelineTileVisibilityContext.Provider value={range}>{children}</TimelineTileVisibilityContext.Provider>
  );
});
