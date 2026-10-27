@echo off
rem Duplo clique: roda o desinstalar.ps1 sem mudar a politica de scripts do Windows (vale so para esta execucao).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0desinstalar.ps1"
echo.
pause
