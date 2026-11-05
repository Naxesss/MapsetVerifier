import { getTimelineX, getTimestampAtPlayhead } from './timelineUtils.ts';

/** Past this the clock is re-anchored outright, as the song has really moved (or stalled). */
const MAX_CLOCK_DRIFT_MS = 60;
/** Share of the drift corrected per frame, so `currentTime` jitter never reaches the cursor. */
const CLOCK_SLEW = 0.08;
/** Stepping through the song only seeks it after this long without a further step. */
const SCRUB_SETTLE_MS = 120;
/** The browser may snap a written scroll position to a physical pixel, so it reads back different. */
const SCROLL_POSITION_TOLERANCE_PX = 1;

type TimelineGeometry = {
  timelineWidth: number;
  startTimeMs: number;
  endTimeMs: number;
  originX: number;
  endPaddingPx: number;
};

type TimelinePlayerCallbacks = {
  onPlayingChange: (playing: boolean) => void;
  onFailedChange: (failed: boolean) => void;
};

/**
 * Plays the song along the timeline. Runs outside of React on purpose: a frame only writes the
 * scroll position and the cursor's compositor-only transform, so playing never re-renders a row.
 *
 * The cursor stays centred in the view both when playing and when paused, which the padding at
 * either end of the timeline allows all the way through it. Scrolling while paused takes the cursor along, which only records the time; the
 * audio is seeked once, on resume, so that scrolling costs nothing and resuming never jumps.
 * Stepping while playing does the same in spirit: the view moves at once and the song follows when
 * the steps stop, as seeking on each of them would only make it stutter.
 */
export class TimelinePlayer {
  private readonly audio: HTMLAudioElement;
  private readonly callbacks: TimelinePlayerCallbacks;

  private scroller: HTMLElement | null = null;
  private cursor: HTMLElement | null = null;
  private geometry: TimelineGeometry = {
    timelineWidth: 1,
    startTimeMs: 0,
    endTimeMs: 1,
    originX: 0,
    endPaddingPx: 0,
  };
  private viewportWidth = 0;
  /** The position last written or seen, to tell a user's scroll from one of our own. */
  private knownScrollLeft = Number.NaN;

  private timeMs = 0;
  /** The audio is not at `timeMs` yet, and has to be seeked before it plays. */
  private seekPending = true;
  private frame: number | null = null;
  /** Between asking to play and the audio really moving, during which the cursor must wait. */
  private waitingForAudio = false;
  /** Set while the song has yet to follow a step made during playback. */
  private scrubTimer: number | null = null;
  private clockMediaMs = 0;
  private clockPerfMs = 0;
  private hasSource = false;
  private failed = false;

  constructor(callbacks: TimelinePlayerCallbacks) {
    this.callbacks = callbacks;
    this.audio = new Audio();
    this.audio.preload = 'auto';
  }

  get playing() {
    return this.frame !== null;
  }

  get currentTimeMs() {
    return this.timeMs;
  }

  setVolume(percent: number) {
    // Comes from the settings file, and assigning anything but a finite number throws.
    if (Number.isFinite(percent)) {
      this.audio.volume = Math.max(0, Math.min(1, percent / 100));
    }
  }

  attach(scroller: HTMLElement, cursor: HTMLElement) {
    this.scroller = scroller;
    this.cursor = cursor;
    this.viewportWidth = scroller.clientWidth;
    scroller.addEventListener('scroll', this.handleScroll, { passive: true });
  }

  detach() {
    this.scroller?.removeEventListener('scroll', this.handleScroll);
    this.scroller = null;
    this.cursor = null;
  }

  setGeometry(geometry: TimelineGeometry) {
    this.geometry = geometry;
    this.timeMs = this.clampTime(this.timeMs);
    // A zoom has just moved the view without telling us, so its position has to be written again.
    this.knownScrollLeft = Number.NaN;
    this.renderView();
  }

  setViewportWidth(width: number) {
    if (width === this.viewportWidth) {
      return;
    }

    this.viewportWidth = width;
    this.knownScrollLeft = Number.NaN;
    this.renderView();
  }

  load(url: string) {
    this.setFailed(false);
    this.hasSource = true;
    this.timeMs = this.getTimeAtViewCentre(this.scroller?.scrollLeft ?? 0);
    this.seekPending = true;

    this.audio.addEventListener('playing', this.handlePlaying);
    this.audio.addEventListener('seeked', this.handleSeeked);
    this.audio.addEventListener('waiting', this.handleWaiting);
    this.audio.addEventListener('seeking', this.handleWaiting);
    this.audio.addEventListener('pause', this.handleStopped);
    this.audio.addEventListener('ended', this.handleStopped);
    this.audio.addEventListener('error', this.handleError);

    this.audio.src = url;
    this.renderView();
  }

  unload() {
    this.audio.removeEventListener('playing', this.handlePlaying);
    this.audio.removeEventListener('seeked', this.handleSeeked);
    this.audio.removeEventListener('waiting', this.handleWaiting);
    this.audio.removeEventListener('seeking', this.handleWaiting);
    this.audio.removeEventListener('pause', this.handleStopped);
    this.audio.removeEventListener('ended', this.handleStopped);
    this.audio.removeEventListener('error', this.handleError);

    this.pause();
    this.hasSource = false;
    this.audio.removeAttribute('src');
    this.audio.load();
  }

  toggle() {
    if (this.playing) {
      this.pause();
    } else {
      this.play();
    }
  }

  /** Moves the cursor and brings it to the centre of the view, with the song following after. */
  seek(timeMs: number) {
    this.timeMs = this.clampTime(timeMs);

    if (this.playing) {
      // Holds the cursor where it was put, or the clock would carry on from the old position.
      this.waitingForAudio = true;
      this.cancelScrub();
      this.scrubTimer = window.setTimeout(this.finishScrub, SCRUB_SETTLE_MS);
    } else {
      this.seekPending = true;
    }

    this.renderView();
  }

  private cancelScrub() {
    if (this.scrubTimer === null) {
      return false;
    }

    window.clearTimeout(this.scrubTimer);
    this.scrubTimer = null;
    return true;
  }

  private readonly finishScrub = () => {
    this.scrubTimer = null;
    this.audio.currentTime = Math.max(0, this.timeMs / 1000);
  };

  private play() {
    if (!this.hasSource || this.failed || this.playing) {
      return;
    }

    if (this.timeMs >= this.geometry.endTimeMs || this.audio.ended) {
      this.timeMs = this.geometry.startTimeMs;
      this.seekPending = true;
    }

    if (this.seekPending) {
      this.audio.currentTime = Math.max(0, this.timeMs / 1000);
      this.seekPending = false;
    }

    this.waitingForAudio = true;
    this.frame = requestAnimationFrame(this.tick);
    this.callbacks.onPlayingChange(true);

    this.audio.play().catch((error: unknown) => {
      // Rejects with an AbortError when paused again before it started, which is not a failure.
      if (error instanceof DOMException && error.name === 'AbortError') {
        return;
      }

      console.error('[Timeline] Could not play audio:', error);
      this.setFailed(true);
      this.pause();
    });
  }

  private pause() {
    if (!this.playing) {
      return;
    }

    this.audio.pause();
    this.finishPlaying();
  }

  private finishPlaying() {
    if (this.frame !== null) {
      cancelAnimationFrame(this.frame);
      this.frame = null;
    }

    // A step the song didn't follow yet stays as the place to resume from.
    if (this.cancelScrub()) {
      this.seekPending = true;
    }

    // Stop exactly where the song did, so that resuming has nothing to seek.
    if (!this.seekPending) {
      this.timeMs = this.clampTime(this.audio.currentTime * 1000);
    }

    this.renderView();
    this.callbacks.onPlayingChange(false);
  }

  private anchorClock() {
    this.clockMediaMs = this.audio.currentTime * 1000;
    this.clockPerfMs = performance.now();
  }

  private readonly tick = (frameTimeMs: number) => {
    this.frame = requestAnimationFrame(this.tick);

    if (this.waitingForAudio) {
      return;
    }

    // `currentTime` moves in uneven steps, which would show as a shaking timeline at high zoom.
    // The steady clock is what's drawn; the song only steers it.
    const drift = this.audio.currentTime * 1000 - this.readClock(performance.now());
    if (Math.abs(drift) > MAX_CLOCK_DRIFT_MS) {
      this.anchorClock();
    } else {
      this.clockMediaMs += drift * CLOCK_SLEW;
    }

    this.timeMs = Math.max(this.timeMs, this.readClock(frameTimeMs));

    if (this.timeMs >= this.geometry.endTimeMs) {
      this.timeMs = this.geometry.endTimeMs;
      this.seekPending = true;
      this.pause();
      return;
    }

    this.renderView();
  };

  private readClock(perfMs: number) {
    return this.clockMediaMs + (perfMs - this.clockPerfMs);
  }

  private clampTime(timeMs: number) {
    return Math.max(this.geometry.startTimeMs, Math.min(this.geometry.endTimeMs, timeMs));
  }

  private getCursorX(timeMs: number) {
    const { timelineWidth, startTimeMs, endTimeMs, originX } = this.geometry;
    const durationMs = Math.max(1, endTimeMs - startTimeMs);
    return originX + getTimelineX(timeMs, startTimeMs, durationMs, timelineWidth);
  }

  private getViewCentreX() {
    return Math.floor(this.viewportWidth / 2);
  }

  private getTimeAtViewCentre(scrollLeft: number) {
    const { timelineWidth, startTimeMs, endTimeMs, originX } = this.geometry;
    return getTimestampAtPlayhead(
      scrollLeft,
      this.getViewCentreX(),
      originX,
      timelineWidth,
      startTimeMs,
      endTimeMs
    );
  }

  private writeCursor(x: number) {
    if (this.cursor) {
      this.cursor.style.transform = `translate3d(${x}px, 0, 0)`;
    }
  }

  /** Puts the view and the cursor where `timeMs` says. Touches no layout-reading property. */
  private renderView() {
    const { scroller } = this;
    if (!scroller || !this.cursor) {
      return;
    }

    // Rounded together with the scroll position, or the cursor would shake against it by a pixel.
    const cursorX = Math.round(this.getCursorX(this.timeMs));
    const { timelineWidth, originX, endPaddingPx } = this.geometry;
    const maxScrollLeft = Math.max(0, originX + timelineWidth + endPaddingPx - this.viewportWidth);
    const scrollLeft = Math.max(0, Math.min(maxScrollLeft, cursorX - this.getViewCentreX()));

    if (scrollLeft !== this.knownScrollLeft) {
      scroller.scrollLeft = scrollLeft;
      this.knownScrollLeft = scrollLeft;
    }

    this.writeCursor(cursorX);
  }

  private readonly handleScroll = () => {
    const { scroller } = this;
    if (!scroller || this.playing) {
      return;
    }

    const scrollLeft = scroller.scrollLeft;
    if (Math.abs(scrollLeft - this.knownScrollLeft) <= SCROLL_POSITION_TOLERANCE_PX) {
      return;
    }

    this.knownScrollLeft = scrollLeft;
    this.timeMs = this.getTimeAtViewCentre(scrollLeft);
    this.seekPending = true;
    this.writeCursor(Math.round(this.getCursorX(this.timeMs)));
  };

  private readonly handlePlaying = () => {
    if (this.scrubTimer !== null) {
      return;
    }

    this.waitingForAudio = false;
    this.anchorClock();
  };

  private readonly handleSeeked = () => {
    if (!this.audio.paused) {
      this.handlePlaying();
    }
  };

  private readonly handleWaiting = () => {
    this.waitingForAudio = true;
  };

  /** Also covers what didn't come from us, such as the keyboard's media keys or the song ending. */
  private readonly handleStopped = () => {
    if (this.playing && this.audio.paused) {
      this.finishPlaying();
    }
  };

  private readonly handleError = () => {
    this.setFailed(true);
    this.pause();
  };

  private setFailed(failed: boolean) {
    if (this.failed !== failed) {
      this.failed = failed;
      this.callbacks.onFailedChange(failed);
    }
  }
}
