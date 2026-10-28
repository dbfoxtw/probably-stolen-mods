@echo off
rem Remove the mod from the game (deletes Mods\dbfoxtw.collect-all-water). Close the game first.
rem The settings (ModConfigs\dbfoxtw.collect-all-water.json in the save folder) are kept.
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
