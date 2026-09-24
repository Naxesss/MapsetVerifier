#!/usr/bin/env bash
# Update the ranking criteria snapshot from ppy/osu-wiki and re-generate the catalogue.
# Run from repository root: ./scripts/update-ranking-criteria.sh [update|reparse] [--commit <sha>]
# See docs/RANKING_CRITERIA.md.

set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT_PATH="$REPO_ROOT/MapsetVerifier.RankingCriteria.Tool/MapsetVerifier.RankingCriteria.Tool.csproj"

ARGS=("$@")
if [ ${#ARGS[@]} -eq 0 ] || [[ "${ARGS[0]}" == --* ]]; then
	ARGS=("update" "${ARGS[@]}")
fi

cd "$REPO_ROOT"
# Build output goes to stderr so stdout only carries the change report.
dotnet build "$PROJECT_PATH" -c Release --verbosity quiet >&2
dotnet run --project "$PROJECT_PATH" -c Release --no-build -- "${ARGS[@]}"
