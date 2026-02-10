@echo off

where bash >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERROR] bash is not available.
    echo.
    echo Bash is required to run this build script.
    echo Please ensure that:
    echo   1. Bash is installed on your system (e.g. Git for Windows, WSL, or MSYS2)
    echo   2. The directory containing bash.exe is added to your PATH environment variable
    exit /b 1
)

bash build-sidecars.sh %*
