'use strict';

/**
 * SemVer prereleases use a hyphen in the version core (before +build metadata):
 * https://semver.org/#spec-item-9
 * Keep aligned with electron-app/src/utils/isSemverPreRelease.ts
 */
function isSemverPreRelease(version) {
  if (version == null || typeof version !== 'string' || version === '' || version === 'unknown')
    return false;
  return version.split('+', 1)[0].includes('-');
}

module.exports = { isSemverPreRelease };
