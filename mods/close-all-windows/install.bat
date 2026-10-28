@echo off
rem Build the mod from source and install it into the game's Mods\dbfoxtw.close-all-windows folder (see README.md). Close the game first.
rem Needs .NET SDK 8+ and Python 3.10+. MelonLoader is not needed (official mod format).
rem First install: enable the mod in the game's mod manager, then restart the game.
rem Add --debug for debug mode (Player.log in the save folder lists the open windows and panels each time you press the key, and what the padlocks and Esc did).
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
