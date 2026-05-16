/**
 * SemVer prereleases use a hyphen in the version core (before +build metadata):
 * https://semver.org/#spec-item-9
 * Keep aligned with electron-app/electron/semverPrerelease.cjs
 */
export function isSemverPreRelease(version: string | null | undefined): boolean {
  if (version == null || version === '' || version === 'unknown') return false;
  return version.split('+', 1)[0].includes('-');
}
