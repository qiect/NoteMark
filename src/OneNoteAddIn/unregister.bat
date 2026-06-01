@echo off
echo ============================================
echo   NoteMark AddIn Unregistration (net48 + RegAsm)
echo ============================================
echo.

set DLL=%~dp0bin\Debug\net48\OneNoteAddIn.dll

echo [1/2] Removing OneNote AddIn registry entries...
reg delete "HKCU\Software\Microsoft\Office\OneNote\AddIns\NoteMark.AddIn" /f 2>nul
echo Registry entries removed.

echo.
echo [2/2] Unregistering COM component with RegAsm...
if exist "%DLL%" (
    "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe" "%DLL%" /unregister
    echo RegAsm unregistration completed.
) else (
    echo DLL not found at %DLL%, skipping RegAsm unregistration.
    echo If the DLL exists elsewhere, run manually:
    echo   RegAsm.exe "path\to\OneNoteAddIn.dll" /unregister
)

echo.
echo Unregistration completed. Please restart OneNote.
pause
