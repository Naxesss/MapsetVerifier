@echo off
set "MV_DETECTED_ARCH=%PROCESSOR_ARCHITECTURE%"

if /i "%MV_DETECTED_ARCH%"=="x86" (
	if not "%PROCESSOR_ARCHITEW6432%"=="" (
		set "MV_DETECTED_ARCH=%PROCESSOR_ARCHITEW6432%"
	)
)

if /i "%MV_DETECTED_ARCH%"=="AMD64" (
	set "MV_TARGET_ARCH=x64"
) else if /i "%MV_DETECTED_ARCH%"=="ARM64" (
	set "MV_TARGET_ARCH=arm64"
) else (
	echo Unsupported architecture: %MV_DETECTED_ARCH%
	exit /b 1
)
