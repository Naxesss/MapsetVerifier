#!/bin/bash
set -e

# Cross-platform sidecar build script for GitHub Actions
# Builds .NET sidecars for specified runtime(s)

echo "[INFO] Starting sidecar build script..."

# Configuration
APP_NAME="MapsetVerifier"
PROJECT_PATH="src/MapsetVerifier.csproj"
BASE_DIR="tauri-app/src-tauri/bin/server"
DIST_DIR="${BASE_DIR}/dist"
STAGING_DIR="${BASE_DIR}/staging"
PUBLISH_CONFIGURATION="${CONFIGURATION:-Release}"
ERROR_COUNT=0

# Runtime to Tauri target suffix mapping
get_target_suffix() {
    case "$1" in
        win-x64)    echo "-x86_64-pc-windows-msvc.exe" ;;
        osx-x64)    echo "-x86_64-apple-darwin" ;;
        osx-arm64)  echo "-aarch64-apple-darwin" ;;
        linux-x64)  echo "-x86_64-unknown-linux-gnu" ;;
        linux-arm64) echo "-aarch64-unknown-linux-gnu" ;;
        *)          echo "" ;;
    esac
}

# Parse arguments - accept runtimes as space-separated list or use default
if [ -n "$RUNTIMES" ]; then
    RUNTIME_LIST="$RUNTIMES"
elif [ $# -gt 0 ]; then
    RUNTIME_LIST="$*"
else
    RUNTIME_LIST="win-x64 osx-x64 osx-arm64 linux-x64 linux-arm64"
fi

echo "[INFO] Using configuration: ${PUBLISH_CONFIGURATION}"
echo "[INFO] Building runtimes: ${RUNTIME_LIST}"

# Prepare output directories
echo "[INFO] Resetting output directories..."
rm -rf "${DIST_DIR}" "${STAGING_DIR}"
mkdir -p "${DIST_DIR}" "${STAGING_DIR}"

echo "[INFO] Dist dir: ${DIST_DIR}"
echo "[INFO] Staging dir: ${STAGING_DIR}"

# Check prerequisites
if ! command -v dotnet &> /dev/null; then
    echo "[ERROR] dotnet CLI not found in PATH"
    exit 1
fi

if [ ! -f "${PROJECT_PATH}" ]; then
    echo "[ERROR] Project file not found: ${PROJECT_PATH}"
    exit 1
fi

# Build each runtime
for RID in ${RUNTIME_LIST}; do
    echo "[INFO] --- Begin runtime ${RID} ---"
    
    TARGET_SUFFIX=$(get_target_suffix "${RID}")
    if [ -z "${TARGET_SUFFIX}" ]; then
        echo "[WARN] Skip unknown RID ${RID}"
        continue
    fi
    
    OUT_SUB="${STAGING_DIR}/${RID}"
    mkdir -p "${OUT_SUB}"
    
    echo "[INFO][${RID}] Running dotnet publish..."
    PUBLISH_LOG="${OUT_SUB}/publish.log"
    
    if ! dotnet publish "${PROJECT_PATH}" \
        -c "${PUBLISH_CONFIGURATION}" \
        -r "${RID}" \
        -o "${OUT_SUB}" \
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
    EXEC_FILE=""
    
    # Look for .exe on Windows builds, or the app name for Unix builds
    if [[ "${RID}" == win-* ]]; then
        EXEC_FILE=$(find "${OUT_SUB}" -maxdepth 1 -name "*.exe" -type f | head -1)
    else
        EXEC_FILE="${OUT_SUB}/${APP_NAME}"
    fi
    
    if [ -z "${EXEC_FILE}" ] || [ ! -f "${EXEC_FILE}" ]; then
        echo "[ERROR] No executable produced for ${RID}. Contents:"
        ls -la "${OUT_SUB}"
        ERROR_COUNT=$((ERROR_COUNT + 1))
        continue
    fi
    
    echo "[INFO][${RID}] Moving to dist as sidecar${TARGET_SUFFIX}"
    mv "${EXEC_FILE}" "${DIST_DIR}/sidecar${TARGET_SUFFIX}"
    
    if [ ! -f "${DIST_DIR}/sidecar${TARGET_SUFFIX}" ]; then
        echo "[ERROR] Executable missing after move: ${DIST_DIR}/sidecar${TARGET_SUFFIX}"
        ERROR_COUNT=$((ERROR_COUNT + 1))
        continue
    fi
    
    echo "[INFO][${RID}] Cleaning staging..."
    rm -rf "${OUT_SUB}"
    
    echo "[INFO] --- End runtime ${RID} (success) ---"
done

# Cleanup staging directory
rm -rf "${STAGING_DIR}"

echo "[INFO] Dist contents:"
ls -la "${DIST_DIR}" 2>/dev/null || echo "[WARN] Dist directory empty or missing"

if [ ${ERROR_COUNT} -gt 0 ]; then
    echo "[SUMMARY] Errors: ${ERROR_COUNT} (script failed)"
    exit 1
else
    echo "[SUMMARY] Success: all runtimes processed with no errors."
    exit 0
fi

