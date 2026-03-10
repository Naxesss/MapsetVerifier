#!/usr/bin/env bash
# Export all beatmap check metadata to JSON.
# Run from repository root: ./scripts/export-checks-metadata.sh [output-path]
# If output-path is omitted, writes to scripts/checks-metadata.json.

set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT_PATH="$REPO_ROOT/MapsetVerifier.Exports/MapsetVerifier.Exports.csproj"
OUT_PATH="${1:-scripts/checks-metadata.json}"

cd "$REPO_ROOT"
dotnet build "$PROJECT_PATH" -c Release --verbosity quiet
dotnet run --project "$PROJECT_PATH" -c Release --no-build -- "$OUT_PATH"
