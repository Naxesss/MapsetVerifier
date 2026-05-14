/**
 * Prefer waiting for enough buffer so play() avoids immediate stalls.
 * Resolves immediately if readiness is already sufficient.
 */
export function waitUntilMediaPlayable(
  audio: HTMLAudioElement,
  timeoutMs = 60_000
): Promise<void> {
  if (audio.readyState >= HTMLMediaElement.HAVE_FUTURE_DATA) {
    return Promise.resolve();
  }

  return new Promise((resolve, reject) => {
    let settled = false;

    const timer = window.setTimeout(() => {
      if (settled) return;
      if (audio.readyState >= HTMLMediaElement.HAVE_FUTURE_DATA) {
        settle(() => resolve());
      } else {
        settle(() => reject(new Error('Audio buffer timeout')));
      }
    }, timeoutMs);

    function settle(fn: () => void) {
      if (settled) return;
      settled = true;
      window.clearTimeout(timer);
      cleanup();
      fn();
    }

    function cleanup() {
      audio.removeEventListener('canplaythrough', onReady);
      audio.removeEventListener('canplay', onReady);
      audio.removeEventListener('error', onError);
    }

    function onReady() {
      settle(() => resolve());
    }

    function onError() {
      settle(() => reject(new Error('Audio playback error')));
    }

    audio.addEventListener('canplaythrough', onReady, { once: true });
    audio.addEventListener('canplay', onReady, { once: true });
    audio.addEventListener('error', onError, { once: true });
  });
}
