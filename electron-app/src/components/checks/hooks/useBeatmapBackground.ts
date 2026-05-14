import { useEffect, useState } from 'react';
import { buildBeatmapImageUrl } from '../../../utils/buildBeatmapFolderPath.ts';

export interface UseBeatmapBackgroundResult {
  bgUrl?: string;
  isLoading: boolean;
}

export function useBeatmapBackground(
  folder?: string,
  songFolder?: string
): UseBeatmapBackgroundResult {
  const [bgUrl, setBgUrl] = useState<string | undefined>(undefined);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    if (!folder) {
      setBgUrl(undefined);
      setLoaded(false);
      return;
    }

    const candidate = buildBeatmapImageUrl(folder, { songFolder, original: true });
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
  }, [folder, songFolder]);

  return { bgUrl, isLoading: !loaded };
}
