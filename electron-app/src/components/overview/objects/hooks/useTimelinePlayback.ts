import { useCallback, useEffect, useLayoutEffect, useRef, useState, type RefObject } from 'react';
import { TimelinePlayer } from '../timelinePlayer.ts';

type UseTimelinePlaybackOptions = {
  audioUrl?: string;
  scrollRef: RefObject<HTMLDivElement | null>;
  timelineWidth: number;
  startTimeMs: number;
  endTimeMs: number;
  originX: number;
  endPaddingPx: number;
  /** A drag in progress has to end before the view starts moving under it. */
  stopPanning: () => void;
};

export type TimelinePlayback = {
  cursorRef: RefObject<HTMLDivElement | null>;
  isPlaying: boolean;
  isUnavailable: boolean;
  toggle: () => void;
  seek: (timeMs: number) => void;
  getTimeMs: () => number;
  setVolume: (percent: number) => void;
};

const NON_TEXT_INPUT_TYPES = new Set(['button', 'checkbox', 'radio', 'range']);

/**
 * Space still has to work after clicking a toggle in the timeline's header, which leaves focus on
 * an input, so only the controls that take typed text keep it to themselves.
 */
function isTypingTarget(target: EventTarget | null) {
  if (!(target instanceof HTMLElement)) {
    return false;
  }

  if (target instanceof HTMLInputElement) {
    return !NON_TEXT_INPUT_TYPES.has(target.type) && !target.readOnly;
  }

  return target instanceof HTMLTextAreaElement || target.isContentEditable;
}

/**
 * Must be called after `usePreserveTimelineScrollOnZoom`, so that its layout effect has already run
 * when this one puts the view back on the cursor.
 */
export function useTimelinePlayback({
  audioUrl,
  scrollRef,
  timelineWidth,
  startTimeMs,
  endTimeMs,
  originX,
  endPaddingPx,
  stopPanning,
}: UseTimelinePlaybackOptions): TimelinePlayback {
  const cursorRef = useRef<HTMLDivElement | null>(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [isUnavailable, setIsUnavailable] = useState(false);
  const [player] = useState(
    () => new TimelinePlayer({ onPlayingChange: setIsPlaying, onFailedChange: setIsUnavailable })
  );

  const hasCursor = !!audioUrl;

  useLayoutEffect(() => {
    const scroller = scrollRef.current;
    const cursor = cursorRef.current;
    if (!scroller || !cursor) {
      return;
    }

    player.attach(scroller, cursor);
    const resizeObserver = new ResizeObserver(() => player.setViewportWidth(scroller.clientWidth));
    resizeObserver.observe(scroller);

    return () => {
      resizeObserver.disconnect();
      player.detach();
    };
  }, [hasCursor, player, scrollRef]);

  useLayoutEffect(() => {
    player.setGeometry({ timelineWidth, startTimeMs, endTimeMs, originX, endPaddingPx });
  }, [endPaddingPx, endTimeMs, hasCursor, originX, player, startTimeMs, timelineWidth]);

  // Picks the time the cursor starts at, which has to happen before the first paint.
  useLayoutEffect(() => {
    if (!audioUrl) {
      return;
    }

    player.load(audioUrl);
    return () => player.unload();
  }, [audioUrl, player]);

  const toggle = useCallback(() => {
    stopPanning();
    player.toggle();
  }, [player, stopPanning]);

  const seek = useCallback((timeMs: number) => player.seek(timeMs), [player]);
  const getTimeMs = useCallback(() => player.currentTimeMs, [player]);
  const setVolume = useCallback((percent: number) => player.setVolume(percent), [player]);

  useEffect(() => {
    if (!audioUrl) {
      return;
    }

    // In the capture phase, so that a focused button or radio doesn't act on the same press. This
    // only exists while the timeline is mounted, which is what keeps the shortcut to this view.
    const handleKeyDown = (event: KeyboardEvent) => {
      if (
        event.code !== 'Space' ||
        event.ctrlKey ||
        event.altKey ||
        event.metaKey ||
        isTypingTarget(event.target) ||
        (event.target instanceof Element && event.target.closest('[role="dialog"]'))
      ) {
        return;
      }

      event.preventDefault();
      event.stopPropagation();

      if (!event.repeat) {
        toggle();
      }
    };

    window.addEventListener('keydown', handleKeyDown, { capture: true });
    return () => window.removeEventListener('keydown', handleKeyDown, { capture: true });
  }, [audioUrl, toggle]);

  return { cursorRef, isPlaying, isUnavailable, toggle, seek, getTimeMs, setVolume };
}
