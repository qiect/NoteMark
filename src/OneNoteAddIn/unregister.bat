@echo off
setlocal

set DLL_PATH=%~dp0bin\Debug\net8.0-windows\OneNoteAddIn.comhost.dll
set PROGID=OneMarkDotNet.AddIn
set ADDIN_KEY=HKCU\Software\Microsoft\Office\OneNote\AddIns\%PROGID%

echo === OneMarkDotNet COM Add-In Unregistration ===
echo.

:: 1. Remove OneNote Add-In registry entry
echo [1/2] Removing OneNote Add-In registry entry...
reg delete "%ADDIN_KEY%" /f >nul 2>&1
echo   Registry entry removed.

:: 2. Unregister COM host
echo [2/2] Unregistering COM host...
regsvr32 /u /s "%DLL_PATH%" 2>nul
echo   COM host unregistered.

echo.
echo === Unregistration complete! ===
pause
