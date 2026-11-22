#!/usr/bin/env bash
# Print the GitHub release text for a stable release, generated from the in-app changelog.
# Run from repository root: ./scripts/release-notes.sh v2.2.0
#
# Only vX.Y.0 tags get text, from electron-app/src/content/changelog/X.Y.md. For anything else
# (prereleases, hotfixes) or a missing changelog file, nothing is printed, so the release text is
# written by hand. See docs/RELEASES.md.
#
# Compared to the in-app changelog, the "## X.Y - date" heading is dropped (GitHub shows both), the
# opening sentence is made bold, and images point at the copies in the repository at this tag, since
# the in-app paths only work inside the app. The app's update dialog shows this text too.

set -euo pipefail

tag="${1:?usage: release-notes.sh <tag>}"
version="${tag#v}"

if [[ ! "$version" =~ ^([0-9]+)\.([0-9]+)\.0$ ]]; then
  exit 0
fi

minor="${BASH_REMATCH[1]}.${BASH_REMATCH[2]}"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
changelog="$root/electron-app/src/content/changelog/$minor.md"

if [ ! -f "$changelog" ]; then
  exit 0
fi

repository="${GITHUB_REPOSITORY:-Naxesss/MapsetVerifier}"
image_base="https://raw.githubusercontent.com/$repository/$tag/electron-app/public/changelog/"

awk -v image_base="$image_base" '
  { sub(/\r$/, "") }

  # Skip the version heading and the blank lines after it, then make the opening sentence bold.
  !started {
    if (NR == 1 && /^## /) next
    if (/^[[:space:]]*$/) next
    started = 1
    print "**" $0 "**"
    next
  }

  {
    gsub(/\]\(changelog\//, "](" image_base)
    print
  }
' "$changelog"
