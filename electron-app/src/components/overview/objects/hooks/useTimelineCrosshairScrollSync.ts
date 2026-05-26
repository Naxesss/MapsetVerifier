import { useCallback, useEffect, useRef, type RefObject } from 'react';
import { LABEL_WIDTH } from '../constants.ts';
import { getPlayheadScrollPadding, getTimestampAtPlayhead } from '../timelineUtils.ts';
import type { TimelineCrosshairState } from '../context/types.ts';

type UseTimelineCrosshairScrollSyncOptions = {
  enabled: boolean;
  scrollRef: RefObject<HTMLDivElement | null>;
  playheadViewportX: number | null;
  timelineWidth: number;
  startTimeMs: number;
  endTimeMs: number;
  setCrosshair: (state: TimelineCrosshairState | null) => void;
};

export function useTimelineCrosshairScrollSync({
  enabled,
  scrollRef,
  playheadViewportX,
  timelineWidth,
  startTimeMs,
  endTimeMs,
  setCrosshair,
}: UseTimelineCrosshairScrollSyncOptions) {
  const rafRef = useRef<number | null>(null);
  const lastRoundedTimestampRef = useRef<number | null>(null);

  const computeTimestampMs = useCallback(() => {
    const scrollElement = scrollRef.current;
    if (!scrollElement || playheadViewportX === null) {
      return null;
    }

    const padding =
      scrollElement.clientWidth > 0
        ? getPlayheadScrollPadding(playheadViewportX, LABEL_WIDTH, scrollElement.clientWidth)
        : undefined;

    return getTimestampAtPlayhead(
      scrollElement.scrollLeft,
      playheadViewportX,
      LABEL_WIDTH,
      timelineWidth,
      startTimeMs,
      endTimeMs,
      padding
    );
  }, [endTimeMs, playheadViewportX, scrollRef, startTimeMs, timelineWidth]);

  const commitCrosshair = useCallback(
    (timestampMs: number, force = false) => {
      const roundedTimestamp = Math.round(timestampMs);

      if (!force && lastRoundedTimestampRef.current === roundedTimestamp) {
        return;
      }

      lastRoundedTimestampRef.current = roundedTimestamp;
      setCrosshair({ timestampMs });
    },
    [setCrosshair]
  );

  const updateCrosshairNow = useCallback(
    (force = false) => {
      if (rafRef.current !== null) {
        cancelAnimationFrame(rafRef.current);
        rafRef.current = null;
      }

      const timestampMs = computeTimestampMs();
      if (timestampMs === null) {
        return;
      }

      commitCrosshair(timestampMs, force);
    },
    [commitCrosshair, computeTimestampMs]
  );

  const scheduleCrosshairUpdate = useCallback(() => {
    if (rafRef.current !== null) {
      return;
    }

    rafRef.current = requestAnimationFrame(() => {
      rafRef.current = null;
      updateCrosshairNow();
    });
  }, [updateCrosshairNow]);

  useEffect(() => {
    return () => {
      if (rafRef.current !== null) {
        cancelAnimationFrame(rafRef.current);
      }
    };
  }, []);

  useEffect(() => {
    if (!enabled || playheadViewportX === null) {
      lastRoundedTimestampRef.current = null;
      setCrosshair(null);
      return;
    }

    updateCrosshairNow(true);
  }, [enabled, playheadViewportX, setCrosshair, timelineWidth, updateCrosshairNow]);

  useEffect(() => {
    if (!enabled || playheadViewportX === null) {
      return;
    }

    const scrollElement = scrollRef.current;
    if (!scrollElement) {
      return;
    }

    const handleScroll = () => {
      scheduleCrosshairUpdate();
    };

    scrollElement.addEventListener('scroll', handleScroll, { passive: true });
    return () => {
      scrollElement.removeEventListener('scroll', handleScroll);
    };
  }, [enabled, playheadViewportX, scheduleCrosshairUpdate, scrollRef]);

  return {
    updateCrosshairNow,
    setCrosshairTimestamp: (timestampMs: number) => {
      commitCrosshair(timestampMs, true);
    },
  };
}
