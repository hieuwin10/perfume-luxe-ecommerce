@echo off
chcp 65001 >nul
title PerfumeLuxe - Chay Dotnet Thuan (http://localhost:5000)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run-all.ps1" -Mode dotnet
pause
