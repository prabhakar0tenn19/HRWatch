@echo off
title HRWatch 2.0 - Office Attendance Sync Tool
color 0b
echo ==========================================================
echo    HRWatch 2.0 - CG Infinity Office Attendance Sync
echo ==========================================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Sync-Office-Attendance.ps1"
echo.
echo Press any key to exit...
pause >nul
