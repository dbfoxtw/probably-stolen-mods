@echo off
rem Build the mod from source and install it into the game (see README.md). Close the game first.
rem Needs MelonLoader 0.7.3 (start the game once after installing it), .NET SDK 8+ and Python 3.10+.
rem Add --debug for debug mode (the log records each night's predicted and actual exchanges).
rem This file is ASCII-only on purpose: cmd.exe parses .bat files byte by byte.
setlocal
cd /d "%~dp0"
chcp 65001 >nul
set PYTHONUTF8=1
set PYTHONIOENCODING=utf-8
where py >nul 2>nul
if not errorlevel 1 (
    py -3 tools\install.py install %*
) else (
    python tools\install.py install %*
)
echo.
pause
