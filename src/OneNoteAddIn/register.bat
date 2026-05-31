@echo off
setlocal

set BUILD_DIR=%~dp0bin\Debug\net8.0-windows
set PUB_DIR=%~dp0bin\Release\net8.0-windows\win-x64\publish
set PROGID=OneMarkDotNet.AddIn
set ADDIN_KEY=HKCU\Software\Microsoft\Office\OneNote\AddIns\%PROGID%

echo === OneMarkDotNet COM Add-In Registration ===
echo.

:: Determine which directory to use
set DLL_DIR=
if exist "%PUB_DIR%\OneNoteAddIn.comhost.dll" (
    set DLL_DIR=%PUB_DIR%
    echo Using PUBLISH directory: %PUB_DIR%
) else if exist "%BUILD_DIR%\OneNoteAddIn.comhost.dll" (
    set DLL_DIR=%BUILD_DIR%
    echo Using BUILD directory: %BUILD_DIR%
) else (
    echo ERROR: No build output found!
    echo.
    echo Please run one of:
    echo   dotnet build -c Debug
    echo   dotnet publish -c Release -r win-x64
    pause
    exit /b 1
)
echo.

:: 1. Check .NET runtime (for non-self-contained builds)
if "%DLL_DIR%"=="%BUILD_DIR%" (
    echo [Check] Verifying .NET 8 Desktop Runtime...
    dotnet --list-runtimes 2>nul | findstr /i "Microsoft.WindowsDesktop.App 8" >nul
    if %errorlevel% neq 0 (
        echo WARNING: .NET 8 Windows Desktop Runtime may not be installed!
        echo          For self-contained build, use: dotnet publish -c Release -r win-x64
        echo.
    ) else (
        echo   .NET 8 Desktop Runtime found.
        echo.
    )
)

:: 2. Unregister old COM entries first
echo [1/3] Cleaning old registration...
regsvr32 /u /s "%DLL_DIR%\OneNoteAddIn.comhost.dll" 2>nul
reg delete "%ADDIN_KEY%" /f >nul 2>&1
echo   Done.
echo.

:: 3. Register COM host (requires admin)
echo [2/3] Registering COM host...
regsvr32 /s "%DLL_DIR%\OneNoteAddIn.comhost.dll"
if %errorlevel% neq 0 (
    echo ERROR: regsvr32 failed. Please run as Administrator.
    echo        Right-click this script and select "Run as administrator"
    pause
    exit /b 1
)
echo   COM host registered successfully.
echo.

:: 4. Add OneNote Add-In registry entry
echo [3/3] Adding OneNote Add-In registry entry...
reg add "%ADDIN_KEY%" /ve /d "OneMarkDotNet - OneNote Markdown Plugin" /f >nul 2>&1
reg add "%ADDIN_KEY%" /v LoadBehavior /t REG_DWORD /d 3 /f >nul 2>&1
reg add "%ADDIN_KEY%" /v FriendlyName /d "OneMarkDotNet" /f >nul 2>&1
reg add "%ADDIN_KEY%" /v Description /d "Markdown rendering and export plugin for OneNote" /f >nul 2>&1
echo   Registry entry added successfully.
echo.

:: 5. Verify registration
echo === Verification ===
reg query "HKCR\CLSID\{B8F2E4A1-3D7C-4F9B-A5E6-8C1D2F3A4B5E}\InprocServer32" >nul 2>&1
if %errorlevel% equ 0 (
    echo   [OK] CLSID registered in HKCR
) else (
    echo   [FAIL] CLSID NOT found in HKCR
)

reg query "%ADDIN_KEY%" /v LoadBehavior >nul 2>&1
if %errorlevel% equ 0 (
    echo   [OK] OneNote Add-In key registered
) else (
    echo   [FAIL] OneNote Add-In key NOT found
)
echo.

echo === Registration complete! ===
echo   Restart OneNote to see the OneMarkDotNet tab.
echo   Check startup log: %APPDATA%\OneMarkDotNet\logs\startup.log
echo.
pause
