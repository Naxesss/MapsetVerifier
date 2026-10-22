import { notifyError } from '../utils/notify.tsx';

/**
 * Opens a link in the browser (or its registered app, e.g. osu://). When that fails, the link is
 * copied instead and a toast says so, so the user can still get there. Never throws.
 */
export async function openExternal(url: string) {
  try {
    if (window.electronAPI?.shell.openExternal) {
      await window.electronAPI.shell.openExternal(url);
    } else {
      window.open(url, '_blank', 'noopener,noreferrer');
    }
  } catch (e) {
    console.error('Failed to open link:', url, e);
    try {
      await navigator.clipboard.writeText(url);
      notifyError("Couldn't open the link. It was copied to the clipboard instead.");
    } catch {
      notifyError(`Couldn't open the link: ${url}`);
    }
  }
}

/** {@link openExternal} for components; the function is stable, so it is safe in dependency lists. */
export function useOpenExternal() {
  return openExternal;
}
