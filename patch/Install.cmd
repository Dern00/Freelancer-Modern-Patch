@echo off
setlocal DisableDelayedExpansion
if not exist "%~dp0Freelancer-Setup.exe" (
  echo Setup is missing. Extract the complete release ZIP into a folder first.
  pause
  exit /b 1
)
start "" "%~dp0Freelancer-Setup.exe"
