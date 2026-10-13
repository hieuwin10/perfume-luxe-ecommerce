@echo off
chcp 65001 >nul
title PerfumeLuxe - All-in-One Launcher (Docker ^& Dotnet)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run-all.ps1" %*
