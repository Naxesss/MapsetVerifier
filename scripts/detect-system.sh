#!/usr/bin/env bash

MV_DETECTED_KERNEL="$(uname -s)"
MV_DETECTED_ARCH="$(uname -m)"

case "$MV_DETECTED_KERNEL" in
	Linux)
		MV_TARGET_OS="linux"
		;;

	Darwin)
		MV_TARGET_OS="mac"
		;;

	*)
		echo "Unsupported or unidentified kernel: $MV_DETECTED_KERNEL"
		exit 1
		;;
esac

case "$MV_DETECTED_ARCH" in
	x86_64)
		MV_TARGET_ARCH="x64"
		;;

	aarch64|arm64)
		MV_TARGET_ARCH="arm64"
		;;

	*)
		echo "Unsupported architecture: $MV_DETECTED_ARCH"
		exit 1
		;;
esac

MV_TARGET_PLATFORM="$MV_TARGET_OS-$MV_TARGET_ARCH"
echo "[INFO] Detected Platform: ${MV_TARGET_PLATFORM}"
