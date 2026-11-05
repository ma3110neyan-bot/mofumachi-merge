@echo off
setlocal
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy RemoteSigned -File "%~dp0verify-font-fix-windows.ps1"
set "FONT_CHECK_RESULT=%ERRORLEVEL%"
echo.
if not "%FONT_CHECK_RESULT%"=="0" echo Verification stopped. Keep the Diagnostics folder. No automatic retry was performed.
pause
exit /b %FONT_CHECK_RESULT%
