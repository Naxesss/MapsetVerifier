@echo off
setlocal enabledelayedexpansion

REM Local Windows sidecar build helper.
REM Builds the .NET sidecar for the requested runtime(s) and lays them out under
REM bin\server\dist\<os>-<arch>\ matching electron-builder's ${os}-${arch} naming.

set APP_NAME=MapsetVerifier
set PROJECT_PATH=src\MapsetVerifier.csproj
set DIST_DIR=bin\server\dist
set STAGING_DIR=bin\server\staging
if "%CONFIGURATION%"=="" set CONFIGURATION=Release

if "%~1"=="" (
    if "%RUNTIMES%"=="" (
        set RUNTIME_LIST=win-x64
    ) else (
        set RUNTIME_LIST=%RUNTIMES%
    )
) else (
    set RUNTIME_LIST=%*
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

set ERROR_COUNT=0

for %%R in (%RUNTIME_LIST%) do (
    echo [INFO] --- Begin runtime %%R ---

    set OUT_DIR_NAME=
    if /I "%%R"=="win-x64"     set OUT_DIR_NAME=win-x64
    if /I "%%R"=="win-arm64"   set OUT_DIR_NAME=win-arm64
    if /I "%%R"=="osx-x64"     set OUT_DIR_NAME=mac-x64
    if /I "%%R"=="osx-arm64"   set OUT_DIR_NAME=mac-arm64
    if /I "%%R"=="linux-x64"   set OUT_DIR_NAME=linux-x64
    if /I "%%R"=="linux-arm64" set OUT_DIR_NAME=linux-arm64

    if "!OUT_DIR_NAME!"=="" (
        echo [WARN] Skip unknown RID %%R
    ) else (
        set STAGE_SUB=%STAGING_DIR%\%%R
        set FINAL_SUB=%DIST_DIR%\!OUT_DIR_NAME!

        mkdir "!STAGE_SUB!" 2>nul
        mkdir "!FINAL_SUB!" 2>nul

        dotnet publish "%PROJECT_PATH%" -c %CONFIGURATION% -r %%R -o "!STAGE_SUB!" ^
            --self-contained true ^
            -p:PublishSingleFile=true ^
            -p:IncludeNativeLibrariesForSelfExtract=true ^
            -p:EnableCompressionInSingleFile=true ^
            -p:UseAppHost=true ^
            -p:DebugType=none ^
            -p:DebugSymbols=false ^
            -p:StripSymbols=true ^
            -p:PublishReadyToRun=false

        if errorlevel 1 (
            echo [ERROR] dotnet publish failed for %%R
            set /a ERROR_COUNT+=1
        ) else (
            REM Windows executable
            if exist "!STAGE_SUB!\%APP_NAME%.exe" (
                move /Y "!STAGE_SUB!\%APP_NAME%.exe" "!FINAL_SUB!\%APP_NAME%.exe" >nul
            REM Unix executable
            ) else if exist "!STAGE_SUB!\%APP_NAME%" (
                move /Y "!STAGE_SUB!\%APP_NAME%" "!FINAL_SUB!\%APP_NAME%" >nul
            ) else (
                echo [ERROR] Expected executable not found for %%R
                echo         Looked for:
                echo         !STAGE_SUB!\%APP_NAME%.exe
                echo         !STAGE_SUB!\%APP_NAME%
                set /a ERROR_COUNT+=1
            )

            rmdir /s /q "!STAGE_SUB!" 2>nul
        )
    )

    echo [INFO] --- End runtime %%R ---
)

if exist "%STAGING_DIR%" rmdir /s /q "%STAGING_DIR%"

echo [INFO] Dist contents:
dir /s /b "%DIST_DIR%" 2>nul

if %ERROR_COUNT% GTR 0 (
    echo [SUMMARY] Errors: %ERROR_COUNT%
    exit /b 1
)

echo [SUMMARY] Success: all runtimes processed with no errors.
exit /b 0
```
