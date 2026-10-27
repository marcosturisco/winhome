@echo off
rem Duplo clique: roda o instalar.ps1 sem mudar a politica de scripts do Windows (vale so para esta execucao).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0instalar.ps1"
echo.
pause
