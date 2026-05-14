import { useCallback, useEffect, useRef, useState } from 'react';
import type { ObjectsTimingSegment } from '../../../../Types.ts';
import { findTimingSegmentForTime, snapTimeToBeatGrid } from '../timingSegmentPlayback.ts';
import { getTimelineX } from '../timelineUtils.ts';
import { LABEL_WIDTH } from '../constants.ts';
import { waitUntilMediaPlayable } from './waitUntilMediaPlayable.ts';

/** Horizontally scroll timeline so `timeMs` sits under the viewport playhead (overlay at this ratio of width). */
const VIEWPORT_PLAYHEAD_X_RATIO = 0.5;
const AUDIO_SYNC_EPSILON_MS = 120;
/** Max rate for syncing playback time React state during playing (smooth scroll outweighs HUD fps). */
const PLAYBACK_UI_EMIT_INTERVAL_MS = 1000 / 30;

export type TimelinePlaybackAudioStatus = 'idle' | 'loading' | 'ready' | 'error';

export interface UseTimelinePlaybackArgs {
  scrollRef: React.RefObject<HTMLDivElement | null>;
  timelineWidth: number;
  startTimeMs: number;
  endTimeMs: number;
  audioUrl: string | null;
  timingSegments: ObjectsTimingSegment[];
  seekSnapDivisor: number | null;
}

export interface UseTimelinePlaybackResult {
  playbackMapTimeMs: number;
  /**
   * Move playhead without touching audio — e.g. while dragging the seek slider.
   * Set `adjustScroll: false` when mapping from pointers on the scrollable timeline
   * (centering scroll each move breaks `clientX + scrollLeft` math).
   */
  scrubToTimeMs: (
    timeMs: number,
    options?: {
      adjustScroll?: boolean;
    }
  ) => void;
  /** Snap (if enabled) and sync audio.currentTime after scrub ends. */
  commitScrub: () => void;
  seek: (timeMs: number) => void;
  isPlaying: boolean;
  togglePlaying: () => void;
  pause: () => void;
  muted: boolean;
  setMuted: (v: boolean) => void;
  volume: number;
  setVolume: (v: number) => void;
  audioStatus: TimelinePlaybackAudioStatus;
  setSeekSnapEnabled: (v: boolean) => void;
  seekSnapEnabled: boolean;
  /** When paused, set playback time from whatever map time is under the fixed viewport playhead. */
  alignPlaybackFromViewportCenter: () => void;
}

export function useTimelinePlayback({
  scrollRef,
  timelineWidth,
  startTimeMs,
  endTimeMs,
  audioUrl,
  timingSegments,
  seekSnapDivisor,
}: UseTimelinePlaybackArgs): UseTimelinePlaybackResult {
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const audioReadyRef = useRef(false);
  const playbackMapTimeMsRef = useRef(startTimeMs);
  const [playbackMapTimeMs, setPlaybackMapTimeMs] = useState(startTimeMs);
  const [isPlaying, setIsPlaying] = useState(false);
  const [audioStatus, setAudioStatus] = useState<TimelinePlaybackAudioStatus>('idle');
  const [muted, setMuted] = useState(false);
  const [volume, setVolume] = useState(1);
  const [seekSnapEnabled, setSeekSnapEnabled] = useState(false);

  const seekSnapEnabledRef = useRef(seekSnapEnabled);
  seekSnapEnabledRef.current = seekSnapEnabled;

  const dimsRef = useRef({ timelineWidth, startTimeMs, endTimeMs });
  dimsRef.current = { timelineWidth, startTimeMs, endTimeMs };

  const isPlayingRef = useRef(isPlaying);
  isPlayingRef.current = isPlaying;

  const rafRef = useRef<number | null>(null);
  const lastFrameTsRef = useRef<number | null>(null);
  const lastUiEmitTsRef = useRef<number | null>(null);

  useEffect(() => {
    let t = playbackMapTimeMsRef.current;
    t = Math.min(Math.max(t, startTimeMs), endTimeMs);
    playbackMapTimeMsRef.current = t;
    setPlaybackMapTimeMs(t);
  }, [startTimeMs, endTimeMs]);

  useEffect(() => {
    audioReadyRef.current = false;
    setIsPlaying(false);
    if (!audioUrl) {
      if (audioRef.current) {
        audioRef.current.pause();
        audioRef.current.removeAttribute('src');
        audioRef.current.load();
      }
      audioRef.current = null;
      setAudioStatus('idle');
      return;
    }

    setAudioStatus('loading');
    const audio = new Audio();
    audio.preload = 'auto';
    audioRef.current = audio;

    const onCanPlay = () => {
      audioReadyRef.current = true;
      setAudioStatus('ready');
    };
    const onErr = () => {
      audioReadyRef.current = false;
      setAudioStatus('error');
    };

    audio.addEventListener('canplay', onCanPlay);
    audio.addEventListener('error', onErr);
    audio.src = audioUrl;
    audio.load();

    return () => {
      audio.removeEventListener('canplay', onCanPlay);
      audio.removeEventListener('error', onErr);
      audio.pause();
      audio.removeAttribute('src');
      audio.load();
      if (audioRef.current === audio) audioRef.current = null;
      audioReadyRef.current = false;
    };
  }, [audioUrl]);

  useEffect(() => {
    const a = audioRef.current;
    if (a)
      a.volume = muted ? 0 : Math.min(1, Math.max(0, volume));
  }, [muted, volume]);

  const applyScrollForTime = useCallback(
    (timeMs: number) => {
      const el = scrollRef.current;
      const { startTimeMs: s, endTimeMs: e, timelineWidth: tw } = dimsRef.current;
      if (!el || tw <= 0) return;
      const d = Math.max(1, e - s);
      const timelineXContent = LABEL_WIDTH + getTimelineX(timeMs, s, d, tw);
      const vw = el.clientWidth;
      const desired = timelineXContent - vw * VIEWPORT_PLAYHEAD_X_RATIO;
      const maxScroll = Math.max(0, el.scrollWidth - vw);
      el.scrollLeft = Math.min(Math.max(0, desired), maxScroll);
    },
    [scrollRef]
  );

  const applyPlaybackTime = useCallback(
    (
      rawTimeMs: number,
      options: {
        snap: boolean;
        syncAudio: boolean;
        /** Defaults to true — set false during pointer-drag scrub on scrolled content */
        scroll?: boolean;
      }
    ) => {
      const { startTimeMs: s, endTimeMs: e } = dimsRef.current;
      let t = Math.min(Math.max(rawTimeMs, s), e);
      if (options.snap && seekSnapDivisor != null && timingSegments.length > 0) {
        const seg = findTimingSegmentForTime(timingSegments, t);
        if (seg) {
          t = snapTimeToBeatGrid(t, seg, seekSnapDivisor);
          t = Math.min(Math.max(t, s), e);
        }
      }
      playbackMapTimeMsRef.current = t;
      setPlaybackMapTimeMs(t);

      if (options.syncAudio) {
        const au = audioRef.current;
        if (au && au.readyState >= HTMLMediaElement.HAVE_METADATA) {
          try {
            au.currentTime = t / 1000;
          } catch {
            /* ignore seek failures while element is still initializing */
          }
        }
      }
      if (options.scroll !== false) {
        applyScrollForTime(t);
      }
    },
    [applyScrollForTime, seekSnapDivisor, timingSegments]
  );

  const scrubToTimeMs = useCallback(
    (rawTimeMs: number, opts?: { adjustScroll?: boolean }) => {
      applyPlaybackTime(rawTimeMs, {
        snap: false,
        syncAudio: false,
        scroll: opts?.adjustScroll !== false,
      });
    },
    [applyPlaybackTime]
  );

  const commitScrub = useCallback(() => {
    applyPlaybackTime(playbackMapTimeMsRef.current, {
      snap: seekSnapEnabled,
      syncAudio: true,
    });
  }, [applyPlaybackTime, seekSnapEnabled]);

  const seek = useCallback(
    (rawTimeMs: number) => {
      applyPlaybackTime(rawTimeMs, {
        snap: seekSnapEnabled && seekSnapDivisor != null,
        syncAudio: true,
      });
    },
    [applyPlaybackTime, seekSnapDivisor, seekSnapEnabled]
  );

  const pause = useCallback(() => {
    setIsPlaying(false);
    audioRef.current?.pause();
  }, []);

  const togglePlaying = useCallback(() => {
    setIsPlaying((p) => !p);
  }, []);

  const setSeekSnap = useCallback((value: boolean) => {
    setSeekSnapEnabled(value);
  }, []);

  const alignPlaybackFromViewportCenter = useCallback(() => {
    if (isPlayingRef.current) return;
    const el = scrollRef.current;
    const { startTimeMs: s, endTimeMs: e, timelineWidth: tw } = dimsRef.current;
    if (!el || tw <= 0) return;
    const vw = el.clientWidth;
    const viewportCenterXContent = el.scrollLeft + vw * VIEWPORT_PLAYHEAD_X_RATIO;
    const relTimelineX = viewportCenterXContent - LABEL_WIDTH;
    const d = Math.max(1, e - s);
    const clampedRel = Math.min(Math.max(0, relTimelineX), tw);
    let tMs = s + (clampedRel / tw) * d;
    tMs = Math.min(Math.max(tMs, s), e);
    applyPlaybackTime(tMs, {
      snap: seekSnapEnabledRef.current && seekSnapDivisor != null,
      syncAudio: true,
    });
  }, [scrollRef, applyPlaybackTime, seekSnapDivisor]);

  useEffect(() => {
    const au = audioRef.current;
    if (!au || !audioUrl) return;

    let cancelled = false;

    void (async () => {
      if (!isPlaying) {
        au.pause();
        return;
      }

      au.currentTime = playbackMapTimeMsRef.current / 1000;

      try {
        await waitUntilMediaPlayable(au);
        if (cancelled) return;
        if (!audioReadyRef.current && au.readyState >= HTMLMediaElement.HAVE_FUTURE_DATA) {
          audioReadyRef.current = true;
          setAudioStatus('ready');
        }
        await au.play();
      } catch {
        if (!cancelled) {
          setIsPlaying(false);
        }
      }
    })();

    return () => {
      cancelled = true;
      au.pause();
    };
  }, [isPlaying, audioUrl]);

  useEffect(() => {
    if (!isPlaying) {
      setPlaybackMapTimeMs(playbackMapTimeMsRef.current);
      lastUiEmitTsRef.current = null;
    }
  }, [isPlaying]);

  useEffect(() => {
    if (!isPlaying) {
      if (rafRef.current != null) {
        cancelAnimationFrame(rafRef.current);
        rafRef.current = null;
      }
      lastFrameTsRef.current = null;
      return;
    }

    const tick = (ts: number) => {
      if (!isPlayingRef.current) return;

      const au = audioRef.current;
      // While isPlaying went true but <audio>.play() hasn't resolved yet (still paused),
      // do not advance the map clock or slam currentTime — that fought play() buffering.
      const pausedWaitingForPlay = au == null || au.paused;

      if (lastFrameTsRef.current == null) {
        lastFrameTsRef.current = ts;
      } else if (!pausedWaitingForPlay) {
        const dt = ts - lastFrameTsRef.current;
        lastFrameTsRef.current = ts;
        playbackMapTimeMsRef.current += dt;
      } else {
        lastFrameTsRef.current = ts;
      }

      const { startTimeMs: s, endTimeMs: e, timelineWidth: tw } = dimsRef.current;
      let t = playbackMapTimeMsRef.current;

      if (t >= e) {
        t = e;
        playbackMapTimeMsRef.current = t;
        setPlaybackMapTimeMs(t);
        setIsPlaying(false);
        audioRef.current?.pause();
        return;
      }

      playbackMapTimeMsRef.current = t;

      const auForSync = audioRef.current;
      if (
        auForSync &&
        !auForSync.paused &&
        audioReadyRef.current
      ) {
        const targetSec = t / 1000;
        if (Math.abs(auForSync.currentTime * 1000 - t) > AUDIO_SYNC_EPSILON_MS) {
          auForSync.currentTime = targetSec;
        }
      }

      const el = scrollRef.current;
      if (el && tw > 0) {
        const d = Math.max(1, e - s);
        const timelineXContent = LABEL_WIDTH + getTimelineX(t, s, d, tw);
        const vw = el.clientWidth;
        const desired = timelineXContent - vw * VIEWPORT_PLAYHEAD_X_RATIO;
        const maxScroll = Math.max(0, el.scrollWidth - vw);
        el.scrollLeft = Math.min(Math.max(0, desired), maxScroll);
      }

      const shouldEmitUi =
        lastUiEmitTsRef.current == null ||
        ts - lastUiEmitTsRef.current >= PLAYBACK_UI_EMIT_INTERVAL_MS;
      if (shouldEmitUi) {
        lastUiEmitTsRef.current = ts;
        setPlaybackMapTimeMs(t);
      }

      rafRef.current = requestAnimationFrame(tick);
    };

    rafRef.current = requestAnimationFrame(tick);
    return () => {
      if (rafRef.current != null) cancelAnimationFrame(rafRef.current);
      rafRef.current = null;
      lastFrameTsRef.current = null;
    };
  }, [isPlaying, scrollRef]);

  return {
    playbackMapTimeMs,
    scrubToTimeMs,
    commitScrub,
    seek,
    isPlaying,
    togglePlaying,
    pause,
    muted,
    setMuted,
    volume,
    setVolume,
    audioStatus,
    setSeekSnapEnabled: setSeekSnap,
    seekSnapEnabled,
    alignPlaybackFromViewportCenter,
  };
}
