@echo off
setlocal

set DLL_PATH=%~dp0bin\Debug\net8.0-windows\OneNoteAddIn.comhost.dll
set PROGID=OneMarkDotNet.AddIn
set ADDIN_KEY=HKCU\Software\Microsoft\Office\OneNote\AddIns\%PROGID%

echo === OneMarkDotNet COM Add-In Registration ===
echo.

:: 1. Register COM host (requires admin)
echo [1/2] Registering COM host...
regsvr32 /s "%DLL_PATH%"
if %errorlevel% neq 0 (
    echo ERROR: regsvr32 failed. Please run as Administrator.
    echo        Right-click this script and select "Run as administrator"
    pause
    exit /b 1
)
echo   COM host registered successfully.

:: 2. Add OneNote Add-In registry entry
echo [2/2] Adding OneNote Add-In registry entry...
reg add "%ADDIN_KEY%" /ve /d "OneMarkDotNet - OneNote Markdown Plugin" /f >nul 2>&1
reg add "%ADDIN_KEY%" /v LoadBehavior /t REG_DWORD /d 3 /f >nul 2>&1
reg add "%ADDIN_KEY%" /v FriendlyName /d "OneMarkDotNet" /f >nul 2>&1
reg add "%ADDIN_KEY%" /v Description /d "Markdown rendering and export plugin for OneNote" /f >nul 2>&1
echo   Registry entry added successfully.

echo.
echo === Registration complete! ===
echo   Restart OneNote to see the OneMarkDotNet tab.
pause
