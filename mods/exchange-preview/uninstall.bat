@echo off
rem Remove the mod from the game (deletes Mods\dbfoxtw.exchange-preview). Close the game first.
rem Its settings and data (ModConfigs\dbfoxtw.exchange-preview.json and ModData\dbfoxtw_exchange-preview in the save folder) are kept.
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
