@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
title Aerial2Mbtiles

if "%~1"=="" (
    echo.
    echo Usage:
    echo   drag and drop a .kmz file onto this script
    echo   drag and drop a raster + .kml pair onto this script
    echo.
    pause
    exit /b 1
)

"%~dp0Aerial2Mbtiles.exe" %*
set "RC=!errorlevel!"

echo.
echo --- Script finished [exit=!RC!] ---
pause
endlocal & exit /b %RC%