import { useCallback, useEffect, useRef, useState } from 'react';
import { useSettings } from '../../../context/SettingsContext.tsx';

/**
 * Plays one short sound at a time (a hit sound) at the timeline's volume. Starting another stops
 * the one playing; leaving the page stops it too.
 */
export function usePreviewSound() {
  const { settings } = useSettings();
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const [playingUrl, setPlayingUrl] = useState<string | null>(null);
  const [failedUrl, setFailedUrl] = useState<string | null>(null);

  const stop = useCallback(() => {
    audioRef.current?.pause();
    audioRef.current = null;
    setPlayingUrl(null);
  }, []);

  useEffect(() => stop, [stop]);

  const play = useCallback(
    (url: string) => {
      stop();
      const audio = new Audio(url);
      const volume = settings.timelineAudioVolume;
      if (Number.isFinite(volume)) audio.volume = Math.max(0, Math.min(1, volume / 100));
      audio.onended = () => {
        if (audioRef.current === audio) stop();
      };
      audioRef.current = audio;
      setPlayingUrl(url);
      setFailedUrl(null);
      audio.play().catch(() => {
        if (audioRef.current !== audio) return;
        stop();
        setFailedUrl(url);
      });
    },
    [settings.timelineAudioVolume, stop]
  );

  return { play, stop, playingUrl, failedUrl };
}
