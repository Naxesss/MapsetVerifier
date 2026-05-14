import { useCallback, useEffect, useRef, useState } from "react";
import { LABEL_WIDTH } from "../constants.ts";

export interface UseTimelineSeekDragArgs {
  scrollRef: React.RefObject<HTMLDivElement | null>;
  timelineWidth: number;
  startTimeMs: number;
  endTimeMs: number;
  playbackMapTimeMs: number;
  enabled: boolean;
  pause: () => void;
  scrubToTimeMs: (
    timeMs: number,
    opts?: {
      adjustScroll?: boolean;
    },
  ) => void;
  commitScrub: () => void;
}

/**
 * Horizontal drag on [data-timeline-seek-zone] scrubs playback; skips scroll pan handling.
 */
export function useTimelineSeekDrag({
  scrollRef,
  timelineWidth,
  startTimeMs,
  endTimeMs,
  playbackMapTimeMs,
  enabled,
  pause,
  scrubToTimeMs,
  commitScrub,
}: UseTimelineSeekDragArgs) {
  const [isSeekDragging, setIsSeekDragging] = useState(false);
  const seekDraggingRef = useRef(false);
  const dragStartClientXRef = useRef(0);
  const dragStartTimeMsRef = useRef(0);
  const scrubRef = useRef(scrubToTimeMs);
  const commitRef = useRef(commitScrub);
  const docCleanupRef = useRef<(() => void) | null>(null);
  scrubRef.current = scrubToTimeMs;
  commitRef.current = commitScrub;

  const dimsRef = useRef({ timelineWidth, startTimeMs, endTimeMs, playbackMapTimeMs, scrollRef });
  dimsRef.current = { timelineWidth, startTimeMs, endTimeMs, playbackMapTimeMs, scrollRef };

  const clientXToTimeMs = useCallback((clientX: number) => {
    const { timelineWidth: tw, startTimeMs: s, endTimeMs: e, scrollRef: ref } = dimsRef.current;
    const el = ref.current;
    if (!el || tw <= 0) return null;
    const rect = el.getBoundingClientRect();
    const xContent = clientX - rect.left + el.scrollLeft;
    const timelineLocal = xContent - LABEL_WIDTH;
    const clamped = Math.max(0, Math.min(timelineLocal, tw));
    const durationMs = Math.max(1, e - s);
    return s + (clamped / tw) * durationMs;
  }, []);

  const tryBeginSeekDrag = useCallback(
    (event: React.MouseEvent<Element>): boolean => {
      if (!enabled || event.button !== 0) return false;

      const target = event.target as HTMLElement | null;
      if (!target?.closest("[data-timeline-seek-zone]")) return false;

      if (target.closest('[data-stop-timeline-pan="true"]')) return false;

      if (clientXToTimeMs(event.clientX) == null) return false;
      seekDraggingRef.current = true;
      setIsSeekDragging(true);
      pause();
      dragStartClientXRef.current = event.clientX;
      // Keep initial click as a pure "grab": no immediate seek jump.
      // Movement applies from the current playhead time.
      dragStartTimeMsRef.current = dimsRef.current.playbackMapTimeMs;

      const onMove = (e: MouseEvent) => {
        if (!seekDraggingRef.current) return;
        const { timelineWidth: tw, startTimeMs: s, endTimeMs: end } = dimsRef.current;
        if (tw <= 0) return;
        const durationMs = Math.max(1, end - s);
        const msPerPixel = durationMs / tw;
        const deltaX = e.clientX - dragStartClientXRef.current;
        // Direct-manipulation drag: content follows cursor direction.
        // Dragging right moves content right => earlier map time.
        const rawT = dragStartTimeMsRef.current - deltaX * msPerPixel;
        scrubRef.current(rawT, { adjustScroll: true });
      };

      const detach = () => {
        document.removeEventListener("mousemove", onMove);
        document.removeEventListener("mouseup", onUp);
        docCleanupRef.current = null;
        setIsSeekDragging(false);
      };

      const onUp = () => {
        if (!seekDraggingRef.current) return;
        seekDraggingRef.current = false;
        detach();
        commitRef.current();
      };

      document.addEventListener("mousemove", onMove);
      document.addEventListener("mouseup", onUp);
      docCleanupRef.current = detach;

      event.preventDefault();
      event.stopPropagation();
      return true;
    },
    [clientXToTimeMs, enabled, pause],
  );

  useEffect(() => {
    return () => {
      seekDraggingRef.current = false;
      docCleanupRef.current?.();
      docCleanupRef.current = null;
      setIsSeekDragging(false);
    };
  }, []);

  return { tryBeginSeekDrag, isSeekDragging };
}
