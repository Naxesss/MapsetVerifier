import { useEffect, useSyncExternalStore } from 'react';

/**
 * Which bar is at the top of the window. Before the app is ready (backend start, setup) the window
 * bar shows; once the nav bar is up it carries the logo, version and window controls, and the window
 * bar hides. Both are the same height with the logo in the same spot, so the swap doesn't move it.
 */
let navBarShown = false;
const listeners = new Set<() => void>();

function setNavBarShown(shown: boolean) {
  navBarShown = shown;
  listeners.forEach((listener) => listener());
}

function subscribe(listener: () => void) {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function useNavBarShown(): boolean {
  return useSyncExternalStore(subscribe, () => navBarShown);
}

/** Called by the nav bar: while it is mounted it is the window's top bar. */
export function useRegisterNavBar() {
  useEffect(() => {
    setNavBarShown(true);
    return () => setNavBarShown(false);
  }, []);
}
