@echo off
pushd "%~dp0.."
powershell.exe -NoLogo -NoProfile -NoExit -ExecutionPolicy Bypass -File "%~dp0prepare-release-signing-windows.ps1"
popd
