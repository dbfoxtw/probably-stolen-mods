@echo off
rem Remove the mod from the game (deletes Mods\dbfoxtw.window-groups). Close the game first.
rem Its settings and data (ModConfigs\dbfoxtw.window-groups.json and ModData\dbfoxtw_window-groups in the save folder: your keys and groups) are kept.
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
