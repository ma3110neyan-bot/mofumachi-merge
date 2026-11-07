@echo off
setlocal
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy RemoteSigned -File "%~dp0verify-recovery-extra-help-windows.ps1"
set "RECOVERY_CHECK_RESULT=%ERRORLEVEL%"
echo.
if not "%RECOVERY_CHECK_RESULT%"=="0" echo Stopped. Diagnostics are preserved. No automatic test or push retry.
pause
exit /b %RECOVERY_CHECK_RESULT%
