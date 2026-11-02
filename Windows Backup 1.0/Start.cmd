@echo off
cd /d "%~dp0"
echo Windows Backup 1.0 - startmenu herstellen
echo 1. Alleen diagnose
echo 2. Startmenu registreren en taakbalk opnieuw starten
choice /c 12 /n /m "Kies 1 of 2: "
if errorlevel 2 goto herstel
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Herstel-Startmenu.ps1" -Actie Diagnose
goto klaar
:herstel
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Herstel-Startmenu.ps1" -Actie Startmenu
:klaar
pause
