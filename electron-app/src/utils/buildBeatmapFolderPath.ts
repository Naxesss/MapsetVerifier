import { BACKEND_BASE_URL } from '../Constants.ts';

export function buildBeatmapFolderPath(songFolder?: string, folder?: string): string | undefined {
  if (!songFolder || !folder) {
    if (folder && /^(?:[A-Za-z]:[\\/]|\\\\|\/)/.test(folder)) {
      return folder;
    }
    return undefined;
  }

  if (/^(?:[A-Za-z]:[\\/]|\\\\|\/)/.test(folder)) {
    return folder;
  }

  const separator = songFolder.includes('\\') && !songFolder.includes('/') ? '\\' : '/';
  const normalizedSongFolder = songFolder.replace(/[\\/]+$/, '');
  const normalizedFolder = folder.replace(/^[\\/]+/, '');

  if (!normalizedSongFolder) {
    return `${separator}${normalizedFolder}`;
  }

  return `${normalizedSongFolder}${separator}${normalizedFolder}`;
}

/** Query string for GET /beatmap/image — pass configured Songs folder when auto-detection on the server may fail. */
export function buildBeatmapImageUrl(
  folder: string,
  options?: { songFolder?: string; original?: boolean }
): string {
  const params = new URLSearchParams();
  params.set('folder', folder);
  if (options?.original) params.set('original', 'true');
  if (options?.songFolder) params.set('songsFolder', options.songFolder);
  return `${BACKEND_BASE_URL}/beatmap/image?${params.toString()}`;
}

/** Query string for GET /beatmap/lazer/image — resolves a lazer beatmapset's background directly from its content-addressed file, keyed by the realm beatmapset id (passed as `folder`). */
export function buildLazerBeatmapImageUrl(
  beatmapSetId: string,
  options?: { original?: boolean; lazerDataDir?: string }
): string {
  const params = new URLSearchParams();
  params.set('id', beatmapSetId);
  if (options?.original) params.set('original', 'true');
  if (options?.lazerDataDir) params.set('lazerDataDir', options.lazerDataDir);
  return `${BACKEND_BASE_URL}/beatmap/lazer/image?${params.toString()}`;
}

/**
 * Prefer the API's backgroundPath (includes cache-bust `v`); fall back to building from set id.
 * Appends lazerDataDir when the client has a custom data folder.
 */
export function resolveLazerBeatmapImageUrl(
  beatmap: { folder: string; backgroundPath?: string },
  lazerDataDir?: string
): string {
  if (beatmap.backgroundPath) {
    const absolute = beatmap.backgroundPath.startsWith('http')
      ? beatmap.backgroundPath
      : `${BACKEND_BASE_URL}${
          beatmap.backgroundPath.startsWith('/')
            ? beatmap.backgroundPath
            : `/${beatmap.backgroundPath}`
        }`;
    if (!lazerDataDir) return absolute;
    const url = new URL(absolute);
    url.searchParams.set('lazerDataDir', lazerDataDir);
    return url.toString();
  }

  return buildLazerBeatmapImageUrl(beatmap.folder, { lazerDataDir });
}
