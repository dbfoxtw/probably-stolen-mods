@echo off
rem Remove Portrait Framework from the game (deletes Mods\dbfoxtw.portrait-framework). Close the game first.
rem Its settings (ModConfigs\dbfoxtw.portrait-framework.json in the save folder) are kept. Portrait packs are separate mods and are not removed.
rem This file is ASCII-only on purpose: cmd.exe parses .bat files byte by byte.
setlocal
cd /d "%~dp0"
chcp 65001 >nul
set PYTHONUTF8=1
set PYTHONIOENCODING=utf-8
where py >nul 2>nul
if not errorlevel 1 (
    py -3 tools\install.py uninstall %*
) else (
    python tools\install.py uninstall %*
)
echo.
pause
