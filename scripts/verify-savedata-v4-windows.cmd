@echo off
setlocal
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy RemoteSigned -File "%~dp0verify-savedata-v4-windows.ps1"
set "SAVEDATA_CHECK_RESULT=%ERRORLEVEL%"
echo.
if not "%SAVEDATA_CHECK_RESULT%"=="0" echo Verification stopped. Preserve the Diagnostics folder. Do not retry automatically.
pause
exit /b %SAVEDATA_CHECK_RESULT%
