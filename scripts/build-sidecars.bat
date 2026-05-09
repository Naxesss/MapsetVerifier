@echo off
setlocal enabledelayedexpansion

REM Local Windows sidecar build helper.
REM Builds the .NET sidecar for the requested runtime(s) and lays them out under
REM bin\server\dist\<os>-<arch>\ matching electron-builder's ${os}-${arch} naming.

set "PROJECT_ROOT=%~dp0.."
set "APP_NAME=MapsetVerifier"
set "PROJECT_PATH=%PROJECT_ROOT%\src\MapsetVerifier.csproj"
set "DIST_DIR=%PROJECT_ROOT%\bin\server\dist"
set "STAGING_DIR=%PROJECT_ROOT%\bin\server\staging"
if "%CONFIGURATION%"=="" set "CONFIGURATION=Release"

if "%~1"=="" (
    if "%RUNTIMES%"=="" (
        set "RUNTIME_LIST=win-x64"
    ) else (
        set "RUNTIME_LIST=%RUNTIMES%"
    )
) else (
    set "RUNTIME_LIST=%*"
)

echo [INFO] Using configuration: %CONFIGURATION%
echo [INFO] Building runtimes: %RUNTIME_LIST%

where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERROR] dotnet CLI not found in PATH
    exit /b 1
)

if not exist "%PROJECT_PATH%" (
    echo [ERROR] Project file not found: %PROJECT_PATH%
    exit /b 1
)

if exist "%DIST_DIR%" rmdir /s /q "%DIST_DIR%"
if exist "%STAGING_DIR%" rmdir /s /q "%STAGING_DIR%"
mkdir "%DIST_DIR%" 2>nul
mkdir "%STAGING_DIR%" 2>nul

set "ERROR_COUNT=0"

for %%R in (%RUNTIME_LIST%) do (
    set "RID=%%I"
    if /I "%%I"=="mac-x64"     set "RID=osx-x64"
    if /I "%%I"=="mac-arm64"   set "RID=osx-arm64"
    
    echo [INFO] --- Begin runtime !RID! ---

    set OUT_DIR_NAME=
    if /I "%%R"=="win-x64"     set "OUT_DIR_NAME=win-x64"
    if /I "%%R"=="win-arm64"   set "OUT_DIR_NAME=win-arm64"
    if /I "%%R"=="osx-x64"     set "OUT_DIR_NAME=mac-x64"
    if /I "%%R"=="osx-arm64"   set "OUT_DIR_NAME=mac-arm64"
    if /I "%%R"=="linux-x64"   set "OUT_DIR_NAME=linux-x64"
    if /I "%%R"=="linux-arm64" set "OUT_DIR_NAME=linux-arm64"

    if "!OUT_DIR_NAME!"=="" (
        echo [WARN] Skip unknown RID !RID!
    ) else (
        set "STAGE_SUB=%STAGING_DIR%\!RID!"
        set "FINAL_SUB=%DIST_DIR%\!OUT_DIR_NAME!"

        mkdir "!STAGE_SUB!" 2>nul
        mkdir "!FINAL_SUB!" 2>nul

        dotnet publish "%PROJECT_PATH%" -c "%CONFIGURATION%" -r "!RID!" -o "!STAGE_SUB!"

        if errorlevel 1 (
            echo [ERROR] dotnet publish failed for !RID!
            set /a ERROR_COUNT+=1
        ) else (
            xcopy /E /I /Y "!STAGE_SUB!\*" "!FINAL_SUB!\" >nul
            rmdir /s /q "!STAGE_SUB!" 2>nul
        )
    )

    echo [INFO] --- End runtime !RID! ---
)

if exist "%STAGING_DIR%" rmdir /s /q "%STAGING_DIR%"

echo [INFO] Dist contents:
dir /s /b "%DIST_DIR%" 2>nul

if !ERROR_COUNT! GTR 0 (
    echo [SUMMARY] Errors: %ERROR_COUNT%
    exit /b 1
)

echo [SUMMARY] Success: all runtimes processed with no errors.
exit /b 0
