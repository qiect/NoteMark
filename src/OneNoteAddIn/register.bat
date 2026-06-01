@echo off
echo ============================================
echo   OneMark AddIn Registration (net48 + RegAsm)
echo ============================================
echo.

set DLL=%~dp0bin\Debug\net48\OneNoteAddIn.dll

if not exist "%DLL%" (
    echo ERROR: DLL not found at %DLL%
    echo Please build the project first.
    pause
    exit /b 1
)

echo [1/3] Registering COM component with RegAsm...
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe" "%DLL%" /codebase
if %ERRORLEVEL% neq 0 (
    echo ERROR: RegAsm registration failed!
    pause
    exit /b 1
)
echo RegAsm registration succeeded.

echo.
echo [2/3] Adding OneNote AddIn registry entries...
reg add "HKCU\Software\Microsoft\Office\OneNote\AddIns\OneMark.AddIn" /ve /d "OneMark - OneNote Markdown Plugin" /f
reg add "HKCU\Software\Microsoft\Office\OneNote\AddIns\OneMark.AddIn" /v FriendlyName /d "OneMark" /f
reg add "HKCU\Software\Microsoft\Office\OneNote\AddIns\OneMark.AddIn" /v Description /d "Markdown rendering and export plugin for OneNote" /f
reg add "HKCU\Software\Microsoft\Office\OneNote\AddIns\OneMark.AddIn" /v LoadBehavior /t REG_DWORD /d 3 /f
echo Registry entries added.

echo.
echo [3/3] Verifying registration...
reg query "HKCU\Software\Microsoft\Office\OneNote\AddIns\OneMark.AddIn" 2>nul
if %ERRORLEVEL% equ 0 (
    echo.
    echo ============================================
    echo   Registration completed successfully!
    echo ============================================
) else (
    echo.
    echo WARNING: Registry verification failed.
)

echo.
echo Please restart OneNote to load the AddIn.
pause
