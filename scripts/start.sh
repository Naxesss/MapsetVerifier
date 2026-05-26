#!/usr/bin/env bash
set -euo pipefail

# POSIX launcher script
# build sidecar and install npm dependencies if needed
# then starts the sidecar and runs the frontend

PROJECT_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

source "${PROJECT_ROOT}/scripts/detect-system.sh"

MV_SERVER_EXEC="${PROJECT_ROOT}/bin/server/dist/${MV_TARGET_PLATFORM}/MapsetVerifier"
MV_SERVER_PID=""
MV_FRONTEND_PID=""

is_running() {
	[[ -n "${1:-}" ]] && kill -0 "$1" >/dev/null 2>&1
}

cleanup() {
	trap - EXIT INT TERM
	
	echo "[INFO] --- Stopping MapsetVerifier ---"
	
	if is_running "${MV_FRONTEND_PID}"; then
		kill "${MV_FRONTEND_PID}" >/dev/null 2>&1 || true
	fi

	if is_running "${MV_SERVER_PID}"; then
		kill "${MV_SERVER_PID}" >/dev/null 2>&1 || true
	fi

	wait "${MV_FRONTEND_PID}" >/dev/null 2>&1 || true
	wait "${MV_SERVER_PID}" >/dev/null 2>&1 || true

	MV_FRONTEND_PID=""
	MV_SERVER_PID=""
}

handle_int() {
	cleanup
	exit 130
}

handle_term() {
	cleanup
	exit 143
}

trap cleanup EXIT 
trap handle_int INT
trap handle_term TERM

if [[ ! -f "${MV_SERVER_EXEC}" ]]; then
	if ! [[ -x "${PROJECT_ROOT}/scripts/build-sidecars.sh" ]]; then
		chmod +x "${PROJECT_ROOT}/scripts/build-sidecars.sh"
	fi
	"${PROJECT_ROOT}/scripts/build-sidecars.sh" "${MV_TARGET_PLATFORM}"
fi

command -v npm >/dev/null 2>&1 || {
	echo "[ERROR] npm CLI not found in PATH."
	exit 1
}

echo "[INFO] Checking npm dependencies..."
cd "${PROJECT_ROOT}/electron-app"
npm install >/dev/null 2>&1 || {
	npm --prefix="${PROJECT_ROOT}/electron-app" install
	echo "[ERROR] failed to install npm dependencies."
	cd -
	exit 1
}
cd -

cd "${PROJECT_ROOT}"
npm install >/dev/null 2>&1 || {
	npm install
	echo "[ERROR] failed to install npm dependencies."
	cd -
	exit 1
}
cd -

echo "[INFO] npm dependencies are up to date."

echo "[INFO] --- Starting MapsetVerifier Server ---"
mkdir -p "${PROJECT_ROOT}/Logs"

if [[ "${MV_TARGET_OS}" == "mac" ]]; then
	# this has worse performance, but it stops it from crashing from read/write to protected memory
	COMPlus_TC_QuickJitForLoops=0 \
	"${MV_SERVER_EXEC}" --urls=http://localhost:5005/ >"${PROJECT_ROOT}/Logs/server.log" 2>&1 &
else
	"${MV_SERVER_EXEC}" --urls=http://localhost:5005/ >"${PROJECT_ROOT}/Logs/server.log" 2>&1 &
fi

MV_SERVER_PID="$!"

echo "[INFO] --- Starting MapsetVerifier Frontend ---"
cd "${PROJECT_ROOT}"
npm run dev >"${PROJECT_ROOT}/Logs/frontend.log" 2>&1 &
cd -
MV_FRONTEND_PID="$!"

wait "${MV_SERVER_PID}" >/dev/null 2>&1 || {
	status="$?"

	case "${status}" in
		130|137|143)
			;;

		*)
			echo "[ERROR] Server exited with status ${status}"
			cleanup
			exit "${status}"
			;;
	esac
}

wait "${MV_FRONTEND_PID}" >/dev/null 2>&1 || {
	status="$?"

	case "${status}" in 
		1)  # Cmd+Q on macOS returns exit code 1
			cleanup
			exit 0
			;;

		130|137|143)
			;;

		*)
			echo "[ERROR] Frontend exited with status ${status}"
			cleanup
			exit "${status}"
			;;
	esac
}

cleanup
exit 0
