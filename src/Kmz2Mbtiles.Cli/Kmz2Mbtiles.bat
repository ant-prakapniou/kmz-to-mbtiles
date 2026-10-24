@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
title Kmz2Mbtiles

if "%~1"=="" (
    echo.
    echo Usage: drag and drop a .kmz file onto this script.
    echo.
    pause
    exit /b 1
)

"%~dp0Kmz2Mbtiles.exe" %*
set "RC=!errorlevel!"

echo.
echo --- Script finished [exit=!RC!] ---
pause
endlocal & exit /b %RC%