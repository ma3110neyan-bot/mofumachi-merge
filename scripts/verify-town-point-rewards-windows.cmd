@echo off
setlocal
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy RemoteSigned -File "%~dp0verify-town-point-rewards-windows.ps1"
set "TOWN_POINT_CHECK_RESULT=%ERRORLEVEL%"
echo.
if not "%TOWN_POINT_CHECK_RESULT%"=="0" echo Stopped. Diagnostics are preserved. No automatic test or push retry.
pause
exit /b %TOWN_POINT_CHECK_RESULT%
