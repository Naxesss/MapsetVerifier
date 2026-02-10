@echo off

bash build-sidecars.sh %*
echo bash exit code=%errorlevel%
exit /b %errorlevel%
