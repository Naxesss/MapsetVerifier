@echo off
setlocal

REM windows-specific launcher script
REM builds the sidecar and installs npm dependencies if required
REM then starts the sidecar and runs the frontend
 
set "PROJECT_ROOT=%~dp0"
call "%PROJECT_ROOT%scripts\detect-system.bat"
if errorlevel 1 exit /b 1

set "MV_SERVER_EXE=%PROJECT_ROOT%bin\server\dist\win-%MV_TARGET_ARCH%\MapsetVerifier.exe"

if not exist "%MV_SERVER_EXE%" (
    call "%PROJECT_ROOT%scripts\build-sidecars.bat" win-%MV_TARGET_ARCH%
    if errorlevel 1 exit /b 1
)

where npm >nul 2>&1
if errorlevel 1 (
	echo [ERROR] npm CLI not found in PATH.
	exit /b 1
)

echo [INFO] Checking npm dependencies...

pushd "%PROJECT_ROOT%electron-app"
call npm install >nul 2>&1
if errorlevel 1 (
	call npm install
	echo [ERROR] failed to install npm dependencies.
	popd
	exit /b 1
)
popd

pushd "%PROJECT_ROOT%"
call npm install >nul 2>&1
if errorlevel 1 (
	call npm install
	echo [ERROR] failed to install npm dependencies. 
	popd
	exit /b 1
)
popd

echo [INFO] npm dependencies are up to date.

echo [INFO] --- Starting MapsetVerifier Server ---
mkdir "%PROJECT_ROOT%Logs"
start "MapsetVerifier Server" /b "%MV_SERVER_EXE%" --urls=http://localhost:5005/ > "%PROJECT_ROOT%Logs\win_server.log" 2>&1

echo [INFO] --- Starting MapsetVerifier Frontend ---
pushd "%PROJECT_ROOT%"
call npm run dev > "%PROJECT_ROOT%Logs\win_frontend.log" 2>&1
popd

echo [INFO] --- Stopping MapsetVerifier Server ---
taskkill /f /im MapsetVerifier.exe >nul 2>&1

endlocal
exit /b 0
