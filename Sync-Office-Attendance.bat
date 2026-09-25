@echo off
title HRWatch 2.0 - Office Attendance Sync Tool
color 0b
echo ==========================================================
echo    HRWatch 2.0 - CG Infinity Office Attendance Sync
echo ==========================================================
echo.

:: 1. Check if Sync-Office-Attendance.ps1 exists in current directory (%~dp0)
set "SCRIPT_PATH=%~dp0Sync-Office-Attendance.ps1"

:: 2. Fallback to C:\HRWatch if running from Desktop or elsewhere
if not exist "%SCRIPT_PATH%" (
    set "SCRIPT_PATH=C:\HRWatch\Sync-Office-Attendance.ps1"
)

:: 3. Fallback to C:\HRWatch\HRWatch
if not exist "%SCRIPT_PATH%" (
    set "SCRIPT_PATH=C:\HRWatch\HRWatch\Sync-Office-Attendance.ps1"
)

if not exist "%SCRIPT_PATH%" (
    color 0c
    echo [ERROR] Could not locate Sync-Office-Attendance.ps1!
    echo Looked in:
    echo   1. %~dp0Sync-Office-Attendance.ps1
    echo   2. C:\HRWatch\Sync-Office-Attendance.ps1
    echo   3. C:\HRWatch\HRWatch\Sync-Office-Attendance.ps1
    echo.
    echo Please make sure HRWatch is located at C:\HRWatch.
    echo.
    pause
    exit /b 1
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_PATH%"
echo.
echo Press any key to exit...
pause >nul
