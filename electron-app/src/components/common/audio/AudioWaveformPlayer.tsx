import {
  alpha,
  Box,
  Group,
  parseThemeColor,
  Stack,
  Text,
  useMantineTheme,
  type MantineColor,
} from '@mantine/core';
import { useElementSize } from '@mantine/hooks';
import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type KeyboardEvent,
  type PointerEvent,
} from 'react';
import { useWaveformPeaks } from './useWaveformPeaks.ts';
import { useSettings } from '../../../context/SettingsContext.tsx';
import TimelinePlaybackControls from '../../overview/objects/components/TimelinePlaybackControls.tsx';
import { BlockSkeleton } from '../LoadingSkeletons.tsx';
import { formatChartTime } from '../TimeAxis.tsx';

interface AudioWaveformPlayerProps {
  /** Where the audio file is served from. */
  url: string;
  /** Colour of the bars, e.g. the file kind's colour in the resources list. */
  color: MantineColor;
  /** Height of the waveform in pixels. */
  height?: number;
}

const BAR_WIDTH = 2;
const BAR_STEP = 3;

/** Short sounds read best in milliseconds, longer ones as minutes and seconds. */
function formatPosition(ms: number, durationMs: number) {
  if (durationMs < 10_000) return `${Math.round(ms).toLocaleString()} ms`;
  return formatChartTime(ms / 1000);
}

/**
 * An audio file drawn as its waveform, to play and to seek in by clicking or dragging across it.
 * Plays at the timeline's volume; leaving it stops the sound. Give it a `key` of the file when
 * the file it shows can change, so playback starts over.
 */
function AudioWaveformPlayer({ url, color, height = 96 }: AudioWaveformPlayerProps) {
  const theme = useMantineTheme();
  const { settings } = useSettings();
  const { data, isLoading, isError } = useWaveformPeaks(url);

  const audioRef = useRef<HTMLAudioElement | null>(null);
  const volumeRef = useRef(settings.timelineAudioVolume);
  const [isPlaying, setIsPlaying] = useState(false);
  const [isUnavailable, setIsUnavailable] = useState(false);
  const [positionMs, setPositionMs] = useState(0);
  const [mediaDurationMs, setMediaDurationMs] = useState(0);
  const [hoverX, setHoverX] = useState<number | null>(null);
  const draggingRef = useRef(false);

  const durationMs = data?.durationMs || mediaDurationMs;

  useEffect(() => {
    const audio = new Audio(url);
    audio.preload = 'metadata';
    audio.volume = Math.max(0, Math.min(1, volumeRef.current / 100));
    audio.onloadedmetadata = () => {
      if (Number.isFinite(audio.duration)) setMediaDurationMs(audio.duration * 1000);
    };
    audio.onended = () => {
      setIsPlaying(false);
      audio.currentTime = 0;
      setPositionMs(0);
    };
    audio.onerror = () => {
      setIsPlaying(false);
      setIsUnavailable(true);
    };
    audioRef.current = audio;

    return () => {
      audio.pause();
      audio.onended = null;
      audio.onerror = null;
      audio.onloadedmetadata = null;
      audio.removeAttribute('src');
      audio.load();
      if (audioRef.current === audio) audioRef.current = null;
    };
  }, [url]);

  // Follow the sound while it plays.
  useEffect(() => {
    if (!isPlaying) return;
    let frame = 0;
    const tick = () => {
      const audio = audioRef.current;
      if (audio && !draggingRef.current) setPositionMs(audio.currentTime * 1000);
      frame = requestAnimationFrame(tick);
    };
    frame = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frame);
  }, [isPlaying]);

  const play = useCallback(() => {
    const audio = audioRef.current;
    if (!audio) return;
    setIsPlaying(true);
    audio.play().catch(() => {
      if (audioRef.current !== audio) return;
      setIsPlaying(false);
      setIsUnavailable(true);
    });
  }, []);

  const toggle = useCallback(() => {
    const audio = audioRef.current;
    if (!audio) return;
    if (audio.paused) {
      play();
    } else {
      audio.pause();
      setIsPlaying(false);
    }
  }, [play]);

  const setVolume = useCallback((value: number) => {
    volumeRef.current = value;
    if (audioRef.current) audioRef.current.volume = Math.max(0, Math.min(1, value / 100));
  }, []);

  const seek = useCallback(
    (ms: number) => {
      const audio = audioRef.current;
      if (!audio || durationMs <= 0) return;
      const clamped = Math.max(0, Math.min(durationMs, ms));
      audio.currentTime = clamped / 1000;
      setPositionMs(clamped);
    },
    [durationMs]
  );

  // Drawing
  const { ref: sizeRef, width } = useElementSize();
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const barColor = parseThemeColor({ color, theme }).value;

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas || width <= 0 || !data) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const ratio = window.devicePixelRatio || 1;
    const pixelWidth = Math.round(width * ratio);
    const pixelHeight = Math.round(height * ratio);
    if (canvas.width !== pixelWidth) canvas.width = pixelWidth;
    if (canvas.height !== pixelHeight) canvas.height = pixelHeight;
    ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
    ctx.clearRect(0, 0, width, height);

    const { peaks } = data;
    const bars = Math.max(1, Math.floor((width + BAR_STEP - BAR_WIDTH) / BAR_STEP));
    const progressX = durationMs > 0 ? (positionMs / durationMs) * width : 0;
    const played = barColor;
    const rest = alpha(barColor, 0.35);

    for (let bar = 0; bar < bars; bar++) {
      const from = Math.floor((bar * peaks.length) / bars);
      const to = Math.max(from + 1, Math.floor(((bar + 1) * peaks.length) / bars));
      let peak = 0;
      for (let i = from; i < to && i < peaks.length; i++) if (peaks[i] > peak) peak = peaks[i];

      const barHeight = Math.max(2, peak * (height - 4));
      const x = bar * BAR_STEP;
      ctx.fillStyle = x + BAR_WIDTH / 2 <= progressX ? played : rest;
      ctx.fillRect(x, (height - barHeight) / 2, BAR_WIDTH, barHeight);
    }

    if (positionMs > 0) {
      ctx.fillStyle = theme.white;
      ctx.fillRect(Math.min(width - 1, progressX), 0, 1, height);
    }
    if (hoverX !== null) {
      ctx.fillStyle = alpha(theme.white, 0.4);
      ctx.fillRect(hoverX, 0, 1, height);
    }
  }, [data, width, height, positionMs, durationMs, hoverX, barColor, theme.white]);

  const msAt = (event: PointerEvent<HTMLDivElement>) => {
    const rect = event.currentTarget.getBoundingClientRect();
    const fraction = Math.max(0, Math.min(1, (event.clientX - rect.left) / rect.width));
    return fraction * durationMs;
  };

  const onPointerDown = (event: PointerEvent<HTMLDivElement>) => {
    if (event.button !== 0 || durationMs <= 0) return;
    event.currentTarget.setPointerCapture(event.pointerId);
    draggingRef.current = true;
    seek(msAt(event));
  };

  const onPointerMove = (event: PointerEvent<HTMLDivElement>) => {
    const rect = event.currentTarget.getBoundingClientRect();
    setHoverX(Math.max(0, Math.min(rect.width, event.clientX - rect.left)));
    if (draggingRef.current) seek(msAt(event));
  };

  const onPointerUp = (event: PointerEvent<HTMLDivElement>) => {
    if (!draggingRef.current) return;
    draggingRef.current = false;
    event.currentTarget.releasePointerCapture(event.pointerId);
    // Clicking a stopped sound plays it from there.
    if (!isPlaying && !isUnavailable) play();
  };

  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const step = durationMs < 10_000 ? durationMs / 20 : 5000;
    if (event.key === 'ArrowRight' || event.key === 'ArrowLeft') {
      event.preventDefault();
      seek(positionMs + (event.key === 'ArrowRight' ? step : -step));
    } else if (event.key === 'Home' || event.key === 'End') {
      event.preventDefault();
      seek(event.key === 'Home' ? 0 : durationMs);
    } else if (event.key === ' ' || event.key === 'Enter') {
      event.preventDefault();
      if (!isUnavailable) toggle();
    }
  };

  // Nothing to draw or play: say so instead of offering a player for silence.
  if (data?.isEmpty) {
    return (
      <Box
        style={{
          height,
          borderRadius: 'var(--mantine-radius-sm)',
          background: 'var(--mantine-color-body)',
          display: 'grid',
          placeItems: 'center',
          padding: 'var(--mantine-spacing-sm)',
          textAlign: 'center',
        }}
      >
        <Stack gap="2xs" align="center">
          <Text size="sm" fw={600}>
            Empty file
          </Text>
          <Text size="sm" c="dimmed">
            It holds no audio, so it plays nothing. This is how a hit sound is silenced.
          </Text>
        </Stack>
      </Box>
    );
  }

  return (
    <Stack gap="xs">
      <Box
        ref={sizeRef}
        role="slider"
        tabIndex={0}
        aria-label="Playback position"
        aria-valuemin={0}
        aria-valuemax={Math.round(durationMs)}
        aria-valuenow={Math.round(positionMs)}
        aria-valuetext={formatPosition(positionMs, durationMs)}
        onPointerDown={onPointerDown}
        onPointerMove={onPointerMove}
        onPointerUp={onPointerUp}
        onPointerCancel={onPointerUp}
        onPointerLeave={() => setHoverX(null)}
        onKeyDown={onKeyDown}
        style={{
          height,
          borderRadius: 'var(--mantine-radius-sm)',
          background: 'var(--mantine-color-body)',
          cursor: durationMs > 0 ? 'pointer' : 'default',
          touchAction: 'none',
          position: 'relative',
        }}
      >
        {isLoading ? (
          <BlockSkeleton height={height} />
        ) : isError ? (
          <Text
            size="sm"
            c="dimmed"
            style={{ position: 'absolute', inset: 0, display: 'grid', placeItems: 'center' }}
          >
            The waveform can&apos;t be drawn for this file.
          </Text>
        ) : (
          <canvas
            ref={canvasRef}
            style={{ display: 'block', width: '100%', height: '100%' }}
            aria-hidden
          />
        )}
      </Box>

      <Group justify="space-between" wrap="nowrap" gap="sm">
        <TimelinePlaybackControls
          isPlaying={isPlaying}
          isUnavailable={isUnavailable}
          toggle={toggle}
          setVolume={setVolume}
          shortcut={null}
          unavailableLabel="This file can't be played here"
        />
        <Text size="sm" c="dimmed" style={{ fontVariantNumeric: 'tabular-nums' }}>
          {formatPosition(positionMs, durationMs)} / {formatPosition(durationMs, durationMs)}
        </Text>
      </Group>
    </Stack>
  );
}

export default AudioWaveformPlayer;
