@echo off
setlocal
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy RemoteSigned -File "%~dp0share-latest-font-diagnostics.ps1"
set "FONT_SHARE_RESULT=%ERRORLEVEL%"
echo.
if not "%FONT_SHARE_RESULT%"=="0" (
    echo Sharing stopped. Read the error above. No automatic retry was performed.
) else (
    echo Diagnostics sharing completed.
)
echo.
pause
exit /b %FONT_SHARE_RESULT%
