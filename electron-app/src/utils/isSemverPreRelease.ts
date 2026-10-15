/**
 * SemVer prereleases use a hyphen in the version core (before +build metadata):
 * https://semver.org/#spec-item-9
 * Keep aligned with electron-app/electron/semverPrerelease.cjs
 */
export function isSemverPreRelease(version: string | null | undefined): boolean {
  if (version == null || version === '' || version === 'unknown') return false;
  return version.split('+', 1)[0].includes('-');
}

/**
 * A `dev` prerelease tag (`2.1.0-dev`, `2.1.0-dev.3`) marks a build that is never meant to be
 * released, independent of whether it is served by Vite or packaged locally.
 */
export function isDevVersion(version: string | null | undefined): boolean {
  if (version == null || version === '' || version === 'unknown') return false;
  const core = version.split('+', 1)[0];
  const separatorIndex = core.indexOf('-');
  if (separatorIndex === -1) return false;
  return (
    core
      .slice(separatorIndex + 1)
      .split('.', 1)[0]
      .toLowerCase() === 'dev'
  );
}
