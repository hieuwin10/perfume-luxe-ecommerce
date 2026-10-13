@echo off
chcp 65001 >nul
title PerfumeLuxe - TAT TOAN BO (Stop Dotnet ^& Docker)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run-all.ps1" -Mode stop
pause
