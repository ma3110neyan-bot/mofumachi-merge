@echo off
setlocal
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy RemoteSigned -File "%~dp0verify-mofu-point-foundation-windows.ps1"
set "POINT_CHECK_RESULT=%ERRORLEVEL%"
echo.
if not "%POINT_CHECK_RESULT%"=="0" echo Stopped. Preserve the mofu-points Diagnostics folder. No automatic retry.
pause
exit /b %POINT_CHECK_RESULT%
