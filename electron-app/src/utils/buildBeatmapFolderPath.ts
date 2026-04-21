import { BACKEND_BASE_URL } from '../Constants.ts';

export function buildBeatmapFolderPath(songFolder?: string, folder?: string): string | undefined {
  if (!songFolder || !folder) {
    return undefined;
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