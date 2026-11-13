import { useEffect, useState } from 'react';
import { isDevVersion } from '../../utils/isSemverPreRelease';

export interface AppVersion {
  /** The release number, without a `.0` patch: `2.2`, `2.2.1`. */
  number: string;
  /** The prerelease tag shown as a badge, e.g. `beta.2` or `dev`; null for a release. */
  tag: string | null;
  /** Dev builds are never released; betas are. */
  tagKind: 'dev' | 'beta' | null;
}

/**
 * Splits a version like `2.3.0-beta.2` into the number and the prerelease tag the top bar shows
 * as a badge. The Vite dev server can run a plain version, so it gets a `dev` tag to tell it
 * apart from a release.
 */
export function splitVersion(version: string, isViteDev: boolean): AppVersion {
  const core = version.split('+', 1)[0];
  const dash = core.indexOf('-');
  const main = dash === -1 ? core : core.slice(0, dash);
  const pre = dash === -1 ? null : core.slice(dash + 1);

  const parts = main.split('.');
  const number = parts.length === 3 && parts[2] === '0' ? parts.slice(0, 2).join('.') : main;

  if (isDevVersion(version) || (isViteDev && !pre)) {
    return { number, tag: pre ?? 'dev', tagKind: 'dev' };
  }
  return { number, tag: pre, tagKind: pre ? 'beta' : null };
}

/** The running app's version, split for the top bar. */
export function useAppVersion(): AppVersion {
  const [version, setVersion] = useState('unknown');

  useEffect(() => {
    window.electronAPI
      ?.getVersion()
      .then(setVersion)
      .catch(() => setVersion('unknown'));
  }, []);

  return splitVersion(version, import.meta.env.DEV);
}
