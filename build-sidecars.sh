#!/bin/bash
set -e

# Cross-platform sidecar build script.
# Builds .NET sidecars for the requested runtime(s) and lays them out under
# bin/server/dist/<os>-<arch>/ using electron-builder's ${os}/${arch} naming.

echo "[INFO] Starting sidecar build script..."

APP_NAME="MapsetVerifier"
PROJECT_PATH="src/MapsetVerifier.csproj"
DIST_DIR="bin/server/dist"
STAGING_DIR="bin/server/staging"
PUBLISH_CONFIGURATION="${CONFIGURATION:-Release}"
ERROR_COUNT=0

# Map dotnet RIDs to electron-builder's ${os}-${arch} directory names.
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

# Accept runtimes via env var, CLI args, or use the default set.
if [ -n "$RUNTIMES" ]; then
    RUNTIME_LIST="$RUNTIMES"
elif [ $# -gt 0 ]; then
    RUNTIME_LIST="$*"
else
    RUNTIME_LIST="win-x64 osx-x64 osx-arm64 linux-x64 linux-arm64"
fi

echo "[INFO] Using configuration: ${PUBLISH_CONFIGURATION}"
echo "[INFO] Building runtimes: ${RUNTIME_LIST}"

echo "[INFO] Resetting output directories..."
rm -rf "${DIST_DIR}" "${STAGING_DIR}"
mkdir -p "${DIST_DIR}" "${STAGING_DIR}"

if ! command -v dotnet &> /dev/null; then
    echo "[ERROR] dotnet CLI not found in PATH"
    exit 1
fi

if [ ! -f "${PROJECT_PATH}" ]; then
    echo "[ERROR] Project file not found: ${PROJECT_PATH}"
    exit 1
fi

for RID in ${RUNTIME_LIST}; do
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
    PUBLISH_LOG="${STAGE_SUB}/publish.log"

    if ! dotnet publish "${PROJECT_PATH}" \
        -c "${PUBLISH_CONFIGURATION}" \
        -r "${RID}" \
        -o "${STAGE_SUB}" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:EnableCompressionInSingleFile=true \
        -p:UseAppHost=true \
        -p:DebugType=none \
        -p:DebugSymbols=false \
        -p:StripSymbols=true \
        -p:PublishReadyToRun=false > "${PUBLISH_LOG}" 2>&1; then
        echo "[ERROR] dotnet publish failed for ${RID}. See ${PUBLISH_LOG}"
        cat "${PUBLISH_LOG}"
        ERROR_COUNT=$((ERROR_COUNT + 1))
        continue
    fi

    echo "[INFO][${RID}] Locate produced executable..."
    if [[ "${RID}" == win-* ]]; then
        EXEC_FILE=$(find "${STAGE_SUB}" -maxdepth 1 -name "*.exe" -type f | head -1)
        TARGET_NAME="${APP_NAME}.exe"
    else
        EXEC_FILE="${STAGE_SUB}/${APP_NAME}"
        TARGET_NAME="${APP_NAME}"
    fi

    if [ -z "${EXEC_FILE}" ] || [ ! -f "${EXEC_FILE}" ]; then
        echo "[ERROR] No executable produced for ${RID}. Contents:"
        ls -la "${STAGE_SUB}"
        ERROR_COUNT=$((ERROR_COUNT + 1))
        continue
    fi

    echo "[INFO][${RID}] Moving to ${FINAL_SUB}/${TARGET_NAME}"
    mv "${EXEC_FILE}" "${FINAL_SUB}/${TARGET_NAME}"

    if [ ! -f "${FINAL_SUB}/${TARGET_NAME}" ]; then
        echo "[ERROR] Executable missing after move: ${FINAL_SUB}/${TARGET_NAME}"
        ERROR_COUNT=$((ERROR_COUNT + 1))
        continue
    fi

    rm -rf "${STAGE_SUB}"
    echo "[INFO] --- End runtime ${RID} (success) ---"
done

rm -rf "${STAGING_DIR}"

echo "[INFO] Dist contents:"
find "${DIST_DIR}" -maxdepth 2 -type f 2>/dev/null || echo "[WARN] Dist directory empty or missing"

if [ ${ERROR_COUNT} -gt 0 ]; then
    echo "[SUMMARY] Errors: ${ERROR_COUNT} (script failed)"
    exit 1
else
    echo "[SUMMARY] Success: all runtimes processed with no errors."
    exit 0
fi
