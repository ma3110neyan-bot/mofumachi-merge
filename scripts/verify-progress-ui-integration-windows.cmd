@echo off
setlocal
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy RemoteSigned -File "%~dp0verify-progress-ui-integration-windows.ps1"
set "PROGRESS_UI_CHECK_RESULT=%ERRORLEVEL%"
echo.
if not "%PROGRESS_UI_CHECK_RESULT%"=="0" echo Stopped. Diagnostics are preserved. No automatic test or push retry.
pause
exit /b %PROGRESS_UI_CHECK_RESULT%
