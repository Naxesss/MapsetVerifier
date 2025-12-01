@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

if defined DEBUG echo [DEBUG] Enabling command echo & echo on

echo [INFO] Starting publish script...

rem Move into main logic after defining helper labels to avoid executing them unintentionally.
goto :main

:logError
rem %1 = code, %2 = message (provide fallbacks to avoid blank output)
set "_ERR_CODE=%~1"
if "%_ERR_CODE%"=="" set "_ERR_CODE=GENERIC"
set "_ERR_MSG=%~2"
if "%_ERR_MSG%"=="" set "_ERR_MSG=Unspecified error"
set "ERROR_OCCURRED=1"
set /a ERROR_COUNT+=1 >nul 2>&1
echo [ERROR][%_ERR_CODE%] %_ERR_MSG%
>&2 echo [ERROR][%_ERR_CODE%] %_ERR_MSG%
exit /b 1

:requirePathExists
rem %1 = path, %2 = code, %3 = description
if not exist "%~1" call :logError %2 "%3 (path not found: %~1)" & exit /b 1
exit /b 0

:ensureRemoveDir
rem %1 = path, %2 = code
if exist "%~1" (
  rmdir /s /q "%~1" 2>nul
  if exist "%~1" call :logError %2 "Failed to remove directory %~1" & exit /b 1
)
exit /b 0

:ensureMakeDir
rem %* directories
for %%D in (%*) do (
  if not exist "%%~D" (
    mkdir "%%~D" 2>nul
    if not exist "%%~D" call :logError DIR_CREATE "Failed to create directory %%~D" & exit /b 1
  )
)
exit /b 0

:publishRuntime
echo [INFO] --- Begin runtime %1 ---
set "RID=%~1"
set "OUT_SUB=%STAGING_DIR%\%RID%"
set "TARGET_SUFFIX="
if "%RID%"=="win-x64"  set "TARGET_SUFFIX=-x86_64-pc-windows-msvc.exe"
if "%RID%"=="osx-x64"  set "TARGET_SUFFIX=-x86_64-apple-darwin"
if "%RID%"=="osx-arm64" set "TARGET_SUFFIX=-aarch64-apple-darwin"
if "%RID%"=="linux-x64" set "TARGET_SUFFIX=-x86_64-unknown-linux-gnu"
if "%RID%"=="linux-arm64" set "TARGET_SUFFIX=-aarch64-unknown-linux-gnu"

if not defined TARGET_SUFFIX (
  echo [WARN] Skip unknown RID %RID%
  exit /b 0
)

echo [INFO][%RID%] Prepare staging directory: %OUT_SUB%
mkdir "%OUT_SUB%" 2>nul
if not exist "%OUT_SUB%" (
  call :logError PUBLISH_DIR "Cannot create output subdirectory for %RID% (%OUT_SUB%)"
  exit /b 1
)

echo [INFO][%RID%] Running dotnet publish...
set "PUBLISH_LOG=%OUT_SUB%\publish.log"
call dotnet publish "%PROJECT_PATH%" -c %PUBLISH_CONFIGURATION% -r %RID% -o "%OUT_SUB%" --self-contained true ^
 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true /p:UseAppHost=true %EXTRA_PUBLISH_ARGS% > "%PUBLISH_LOG%" 2>&1
if errorlevel 1 (
  call :logError PUBLISH_FAIL "dotnet publish failed for %RID%. See %PUBLISH_LOG%"
  exit /b 1
)

echo [INFO][%RID%] Locate produced executable...
set "EXEC_FILE="
for %%F in ("%OUT_SUB%\*.exe") do (
  set "EXEC_FILE=%%~nxF"
  goto :gotExec
)
if not defined EXEC_FILE if exist "%OUT_SUB%\%APP_NAME%" (
  set "EXEC_FILE=%APP_NAME%"
)
:gotExec
if not defined EXEC_FILE (
  call :logError NO_EXEC "No single-file executable produced for %RID%. Contents:"
  dir /b "%OUT_SUB%"
  echo [HINT] Ensure project at %PROJECT_PATH% has ^<OutputType^>Exe^</OutputType^> and a Program entry point.
  exit /b 1
)

echo [INFO][%RID%] Moving to dist as sidecar%TARGET_SUFFIX%
move /y "%OUT_SUB%\%EXEC_FILE%" "%DIST_DIR%\sidecar%TARGET_SUFFIX%" >nul 2>&1
if errorlevel 1 (
  call :logError MOVE_FAIL "Failed to move executable for %RID% (from %OUT_SUB% to %DIST_DIR%)"
  exit /b 1
)

rem Verify the file is accessible
if not exist "%DIST_DIR%\sidecar%TARGET_SUFFIX%" (
  call :logError EXEC_MISSING "Executable missing after move: %DIST_DIR%\sidecar%TARGET_SUFFIX%"
  exit /b 1
)
>"%DIST_DIR%\__read_test.tmp" (echo test) 2>nul & del /q "%DIST_DIR%\__read_test.tmp" 2>nul
if errorlevel 1 (
  call :logError EXEC_ACCESS "Failed to perform simple write test in dist; check permissions on %DIST_DIR%"
  exit /b 1
)

echo [INFO][%RID%] Cleaning staging...
if not defined KEEP_PDBS rmdir /s /q "%OUT_SUB%" 2>nul
if exist "%OUT_SUB%" (
  call :logError CLEAN_FAIL "Failed to clean staging directory for %RID% (%OUT_SUB%)"
  exit /b 1
)

echo [INFO] --- End runtime %RID% (success) ---
exit /b 0

:main
rem ===================== Configuration =====================
set "APP_NAME=MapsetVerifier"
set "PROJECT_PATH=src\MapsetVerifier.csproj"
set "BASE_DIR=tauri-app\src-tauri\bin\server"
set "DIST_DIR=%BASE_DIR%\dist"
set "STAGING_DIR=%BASE_DIR%\staging"
set "PUBLISH_CONFIGURATION=Release"
if defined CONFIGURATION set "PUBLISH_CONFIGURATION=%CONFIGURATION%"
if defined PUBLISH_CONFIGURATION_OVERRIDE set "PUBLISH_CONFIGURATION=%PUBLISH_CONFIGURATION_OVERRIDE%"
set "ERROR_OCCURRED="
set "ERROR_COUNT=0"

echo [INFO] Using configuration: %PUBLISH_CONFIGURATION%
rem Prepare output directories
echo [INFO] Resetting output directories...
call :ensureRemoveDir "%DIST_DIR%" DIST_REMOVE || goto :abort
call :ensureRemoveDir "%STAGING_DIR%" STAGING_REMOVE || goto :abort
call :ensureMakeDir "%DIST_DIR%" "%STAGING_DIR%" || goto :abort

echo [INFO] Dist dir: %DIST_DIR%
echo [INFO] Staging dir: %STAGING_DIR%

where dotnet >nul 2>&1 || (call :logError DOTNET_MISSING "dotnet CLI not found in PATH" & goto :abort)
call :requirePathExists "%PROJECT_PATH%" PROJECT_FILE "Project file missing" || goto :abort

if defined RUNTIMES_OVERRIDE (set "RUNTIMES=%RUNTIMES_OVERRIDE%") else (set "RUNTIMES=win-x64 osx-x64 osx-arm64 linux-x64 linux-arm64")
if "%RUNTIMES%"=="" call :logError RUNTIMES_EMPTY "No runtimes specified" & goto :abort
echo [INFO] Building runtimes: %RUNTIMES%

for %%R in (%RUNTIMES%) do echo [INFO] Queue runtime %%R
for %%R in (%RUNTIMES%) do call :publishRuntime %%R

echo [INFO] Runtime publish loop completed.

if defined ERROR_OCCURRED goto hasErrors

echo [INFO] All runtimes published successfully.
goto afterErrorCheck

:hasErrors
echo [INFO] Completed with %ERROR_COUNT% error(s).
if exist "%STAGING_DIR%" rmdir /s /q "%STAGING_DIR%"
set "HAS_ERRORS=1"

:afterErrorCheck
goto :postPublish

:postPublish
echo [INFO] Dist contents:
if not exist "%DIST_DIR%" call :logError DIST_MISSING "Dist directory missing: %DIST_DIR%"
if exist "%DIST_DIR%" (
  dir /b "%DIST_DIR%"
  if errorlevel 1 call :logError DIST_LIST "Failed to list dist directory"
)
if exist "%DIST_DIR%\*" (
  echo [INFO] Artifacts detected in dist.
) else (
  call :logError DIST_EMPTY "No artifacts produced in %DIST_DIR%"
)

goto :end

:abort
echo [INFO] Aborting early due to previous error(s).

:end
if defined ERROR_OCCURRED (
  echo [SUMMARY] Errors: %ERROR_COUNT% (script failed)
  exit /b 1
) else (
  echo [SUMMARY] Success: all runtimes processed with no errors.
  exit /b 0
)
