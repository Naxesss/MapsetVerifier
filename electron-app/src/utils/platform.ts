export function isMacPlatform(): boolean {
  if (typeof window === 'undefined') {
    return false;
  }

  const electronPlatform = window.electronAPI?.platform;
  if (electronPlatform) {
    return electronPlatform === 'darwin';
  }

  return /Mac|iPhone|iPod|iPad/i.test(navigator.userAgent);
}

export function isWindowsPlatform(): boolean {
  if (typeof window === 'undefined') {
    return false;
  }

  const electronPlatform = window.electronAPI?.platform;
  if (electronPlatform) {
    return electronPlatform === 'win32';
  }

  return /Win/i.test(navigator.userAgent);
}
