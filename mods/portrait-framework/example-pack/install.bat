@echo off
rem Build the Portrait Framework example pack and install it into the game's Mods\dbfoxtw.portrait-framework.example folder. Close the game first.
rem Needs .NET SDK 8+ and Python 3.10+. Also install Portrait Framework (mods\portrait-framework in this repo).
rem First install: enable the pack and Portrait Framework in the game's mod manager, then restart the game.
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
