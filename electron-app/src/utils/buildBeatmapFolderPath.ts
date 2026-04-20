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