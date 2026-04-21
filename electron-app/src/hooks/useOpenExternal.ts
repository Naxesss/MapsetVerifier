import { useCallback } from 'react';

export function useOpenExternal() {
  return useCallback(async (url: string) => {
    if (window.electronAPI?.shell.openExternal) {
      await window.electronAPI.shell.openExternal(url);
    } else {
      window.open(url, '_blank', 'noopener,noreferrer');
    }
  }, []);
}
