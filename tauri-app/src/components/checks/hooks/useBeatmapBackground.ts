import { useEffect, useState } from 'react';

export interface UseBeatmapBackgroundResult {
  bgUrl?: string;
  isLoaded: boolean;
}

export function useBeatmapBackground(folder?: string): UseBeatmapBackgroundResult {
  const [bgUrl, setBgUrl] = useState<string | undefined>(undefined);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    if (!folder) {
      setBgUrl(undefined);
      setLoaded(false);
      return;
    }

    const candidate = `http://localhost:5005/beatmap/image?folder=${folder}&original=true`;
    let cancelled = false;
    const img = new Image();
    img.onload = () => {
      if (!cancelled) {
        setBgUrl(candidate);
        setLoaded(true);
      }
    };
    img.onerror = () => {
      if (!cancelled) {
        setBgUrl(undefined);
        setLoaded(true); // finished attempt
      }
    };
    img.src = candidate;

    return () => {
      cancelled = true;
      setBgUrl(undefined);
      setLoaded(false);
    };
  }, [folder]);

  return { bgUrl, isLoaded: loaded };
}
