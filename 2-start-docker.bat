@echo off
chcp 65001 >nul
title PerfumeLuxe - Chay Docker Full-Stack (http://localhost:5000)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run-all.ps1" -Mode docker
pause
