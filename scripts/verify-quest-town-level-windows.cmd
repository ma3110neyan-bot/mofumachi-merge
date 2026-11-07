@echo off
setlocal
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy RemoteSigned -File "%~dp0verify-quest-town-level-windows.ps1"
set "QUEST_TOWN_CHECK_RESULT=%ERRORLEVEL%"
echo.
if not "%QUEST_TOWN_CHECK_RESULT%"=="0" echo Stopped. Diagnostics are preserved. No automatic test or push retry.
pause
exit /b %QUEST_TOWN_CHECK_RESULT%
