import { useEffect, useState } from 'react';
import { buildBeatmapImageUrl } from '../../../utils/buildBeatmapFolderPath.ts';

export interface UseBeatmapBackgroundResult {
  bgUrl?: string;
  isLoading: boolean;
}

/** Backgrounds that loaded before, so coming back to a mapset shows its art at once. */
const loadedUrls = new Set<string>();

export function useBeatmapBackground(
  folder?: string,
  songFolder?: string
): UseBeatmapBackgroundResult {
  const [, setLoadedCandidate] = useState<string | undefined>(undefined);
  const [failedCandidate, setFailedCandidate] = useState<string | undefined>(undefined);
  const candidate = folder
    ? buildBeatmapImageUrl(folder, { songFolder, original: true })
    : undefined;

  useEffect(() => {
    if (!candidate || loadedUrls.has(candidate)) return;

    let cancelled = false;
    const img = new Image();

    img.onload = () => {
      loadedUrls.add(candidate);
      if (!cancelled) setLoadedCandidate(candidate);
    };
    img.onerror = () => {
      // Not remembered: the next mount tries again.
      if (!cancelled) setFailedCandidate(candidate);
    };
    img.src = candidate;

    return () => {
      cancelled = true;
    };
  }, [candidate]);

  const loaded = !!candidate && loadedUrls.has(candidate);

  return {
    bgUrl: loaded ? candidate : undefined,
    isLoading: !loaded && (!candidate || failedCandidate !== candidate),
  };
}
