#!/bin/bash
set -euo pipefail

echo "[INFO] Starting sidecar build script..."

APP_NAME="MapsetVerifier"
PROJECT_PATH="src/MapsetVerifier.csproj"
DIST_DIR="bin/server/dist"
STAGING_DIR="bin/server/staging"
PUBLISH_CONFIGURATION="${CONFIGURATION:-Release}"
ERROR_COUNT=0

get_dir_name() {
    case "$1" in
        win-x64)     echo "win-x64" ;;
        win-arm64)   echo "win-arm64" ;;
        osx-x64)     echo "mac-x64" ;;
        osx-arm64)   echo "mac-arm64" ;;
        linux-x64)   echo "linux-x64" ;;
        linux-arm64) echo "linux-arm64" ;;
        *)           echo "" ;;
    esac
}

if [ -n "${RUNTIMES:-}" ]; then
    read -r -a RUNTIME_LIST <<< "${RUNTIMES}"
elif [ $# -gt 0 ]; then
    RUNTIME_LIST=("$@")
else
    RUNTIME_LIST=(win-x64 osx-x64 osx-arm64 linux-x64 linux-arm64)
fi

echo "[INFO] Using configuration: ${PUBLISH_CONFIGURATION}"
echo "[INFO] Building runtimes: ${RUNTIME_LIST[*]}"

command -v dotnet >/dev/null 2>&1 || {
    echo "[ERROR] dotnet CLI not found in PATH"
    exit 1
}

[[ -f "${PROJECT_PATH}" ]] || {
    echo "[ERROR] Project file not found: ${PROJECT_PATH}"
    exit 1
}

rm -rf "${DIST_DIR}" "${STAGING_DIR}"
mkdir -p "${DIST_DIR}" "${STAGING_DIR}"

for RID in "${RUNTIME_LIST[@]}"; do
    echo "[INFO] --- Begin runtime ${RID} ---"

    OUT_DIR_NAME=$(get_dir_name "${RID}")
    if [ -z "${OUT_DIR_NAME}" ]; then
        echo "[WARN] Skip unknown RID ${RID}"
        continue
    fi

    STAGE_SUB="${STAGING_DIR}/${RID}"
    FINAL_SUB="${DIST_DIR}/${OUT_DIR_NAME}"

    mkdir -p "${STAGE_SUB}" "${FINAL_SUB}"

    echo "[INFO][${RID}] Running dotnet publish..."

    if ! dotnet publish "${PROJECT_PATH}" \
        -c "${PUBLISH_CONFIGURATION}" \
        -r "${RID}" \
        -o "${STAGE_SUB}"; then

        echo "[ERROR] dotnet publish failed for ${RID}"
        ERROR_COUNT=$((ERROR_COUNT + 1))
        continue
    fi

    echo "[INFO][${RID}] Copying full publish output..."
    cp -R "${STAGE_SUB}/." "${FINAL_SUB}/"
    rm -rf "${STAGE_SUB}"
    echo "[INFO] --- End runtime ${RID} ---"
done

rm -rf "${STAGING_DIR}"

echo "[INFO] Dist contents:"
find "${DIST_DIR}" -maxdepth 3 -type f 2>/dev/null || true

if [ "${ERROR_COUNT}" -gt 0 ]; then
    echo "[SUMMARY] Errors: ${ERROR_COUNT}"
    exit 1
else
    echo "[SUMMARY] Success: all runtimes processed with no errors."
    exit 0
fi
