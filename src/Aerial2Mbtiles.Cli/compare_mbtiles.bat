@echo off
setlocal

if "%~2"=="" (
    echo.
    echo Usage: drag and drop TWO .mbtiles files onto this script.
    echo.
    echo Or run from a console:
    echo   compare_mbtiles.bat file1.mbtiles file2.mbtiles
    echo.
    pause
    exit /b 1
)

python "%~dp0compare_mbtiles.py" "%~1" "%~2"
echo.
pause
endlocal