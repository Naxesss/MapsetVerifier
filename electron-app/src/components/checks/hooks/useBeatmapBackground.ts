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
  const [loadedCandidate, setLoadedCandidate] = useState<string | undefined>(undefined);
  const candidate = folder
    ? buildBeatmapImageUrl(folder, { songFolder, original: true })
    : undefined;

  useEffect(() => {
    if (!candidate) return;

    let cancelled = false;
    const img = new Image();

    img.onload = () => {
      if (!cancelled) {
        setBgUrl(candidate);
        setLoadedCandidate(candidate);
      }
    };
    img.onerror = () => {
      if (!cancelled) {
        setBgUrl(undefined);
        setLoadedCandidate(candidate); // finished attempt
      }
    };
    img.src = candidate;

    return () => {
      cancelled = true;
    };
  }, [candidate]);

  const loaded = !!candidate && loadedCandidate === candidate;

  return { bgUrl: loaded ? bgUrl : undefined, isLoading: !loaded };
}
