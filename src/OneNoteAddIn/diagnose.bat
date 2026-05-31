@echo off
setlocal enabledelayedexpansion

echo ============================================
echo  OneMarkDotNet COM Add-In Diagnostic Tool
echo ============================================
echo.

set PROGID=OneMarkDotNet.AddIn
set CLSID={B8F2E4A1-3D7C-4F9B-A5E6-8C1D2F3A4B5E}
set ADDIN_KEY=HKCU\Software\Microsoft\Office\OneNote\AddIns\%PROGID%
set BUILD_DIR=%~dp0bin\Debug\net8.0-windows
set PUB_DIR=%~dp0bin\Release\net8.0-windows\win-x64\publish

echo [1] Checking .NET 8 Runtime...
dotnet --list-runtimes 2>nul | findstr /i "Microsoft.WindowsDesktop.App 8" >nul
if %errorlevel% equ 0 (
    echo     [OK] .NET 8 Windows Desktop Runtime found
    for /f "tokens=2" %%v in ('dotnet --list-runtimes 2^>nul ^| findstr /i "Microsoft.WindowsDesktop.App 8"') do echo     Version: %%v
) else (
    echo     [FAIL] .NET 8 Windows Desktop Runtime NOT found!
    echo     Download from: https://dotnet.microsoft.com/download/dotnet/8.0
    echo     You need the "Windows Desktop Runtime" x64 version
)
echo.

echo [2] Checking build output directory...
set DLL_DIR=
if exist "%PUB_DIR%\OneNoteAddIn.comhost.dll" (
    set DLL_DIR=%PUB_DIR%
    echo     [OK] Publish directory: %PUB_DIR%
) else if exist "%BUILD_DIR%\OneNoteAddIn.comhost.dll" (
    set DLL_DIR=%BUILD_DIR%
    echo     [OK] Build directory: %BUILD_DIR%
) else (
    echo     [FAIL] comhost.dll not found in build or publish directory!
    echo     Build dir: %BUILD_DIR%
    echo     Publish dir: %PUB_DIR%
    echo.
    echo     Files in build dir:
    if exist "%BUILD_DIR%" (
        dir /b "%BUILD_DIR%\*.dll" "%BUILD_DIR%\*.json" 2>nul
    ) else (
        echo     (directory does not exist)
    )
)
echo.

echo [3] Checking required files...
for %%f in (OneNoteAddIn.dll OneNoteAddIn.comhost.dll OneNoteAddIn.deps.json OneNoteAddIn.runtimeconfig.json) do (
    if exist "%BUILD_DIR%\%%f" (
        echo     [OK] %%f
    ) else (
        echo     [FAIL] %%f MISSING
    )
)
echo.

echo [4] Checking OneNote Add-In registry...
reg query "%ADDIN_KEY%" >nul 2>&1
if %errorlevel% equ 0 (
    echo     [OK] Add-In registry key exists
    for /f "tokens=3" %%v in ('reg query "%ADDIN_KEY%" /v LoadBehavior 2^>nul ^| findstr /i "LoadBehavior"') do (
        echo     LoadBehavior = %%v
        if "%%v"=="0x2" echo     *** LoadBehavior=2 means: OneNote tried to load but FAILED ***
        if "%%v"=="0x3" echo     LoadBehavior=3 means: Load at startup (correct)
        if "%%v"=="0x0" echo     LoadBehavior=0 means: Disabled
    )
) else (
    echo     [FAIL] Add-In registry key NOT found
)
echo.

echo [5] Checking COM CLSID registration - InprocServer32 path...
reg query "HKCR\CLSID\%CLSID%\InprocServer32" >nul 2>&1
if %errorlevel% equ 0 (
    echo     [OK] CLSID registered
    for /f "tokens=3*" %%v in ('reg query "HKCR\CLSID\%CLSID%\InprocServer32" /ve 2^>nul ^| findstr /r "^[^ ]"') do (
        echo     InprocServer32 = %%v
        if exist "%%v" (
            echo     [OK] File exists at registered path
        ) else (
            echo     [FAIL] File does NOT exist at registered path!
            echo     This is likely the problem - re-run regsvr32 to fix
        )
    )
) else (
    echo     [FAIL] CLSID NOT registered
)
echo.

echo [6] Checking ProgId registration...
reg query "HKCR\%PROGID%\CLSID" >nul 2>&1
if %errorlevel% equ 0 (
    echo     [OK] ProgId registered
) else (
    echo     [FAIL] ProgId NOT registered
)
echo.

echo [7] Checking OneNote architecture (32-bit vs 64-bit)...
set ONENOTE_PATH=
for %%p in (
    "C:\Program Files\Microsoft Office\root\Office16\ONENOTE.EXE"
    "C:\Program Files (x86)\Microsoft Office\root\Office16\ONENOTE.EXE"
    "C:\Program Files\Microsoft Office\Office16\ONENOTE.EXE"
    "C:\Program Files (x86)\Microsoft Office\Office16\ONENOTE.EXE"
) do (
    if exist %%p (
        set ONENOTE_PATH=%%~p
        echo     [OK] Found: %%~p
    )
)
if defined ONENOTE_PATH (
    powershell -command "$p = '%ONENOTE_PATH%'; $b = [System.IO.File]::ReadAllBytes($p); $peOffset = [BitConverter]::ToInt32($b, 60); $machine = [BitConverter]::ToUInt16($b, $peOffset + 4); if ($machine -eq 0x8664) { Write-Host '    OneNote architecture: 64-bit (x64)' } elseif ($machine -eq 0x14c) { Write-Host '    OneNote architecture: 32-bit (x86) *** MISMATCH WARNING ***' } else { Write-Host '    OneNote architecture: Unknown (' $machine ')' }" 2>nul
)
echo.

echo [8] Checking comhost.dll architecture...
if defined DLL_DIR (
    if exist "%DLL_DIR%\OneNoteAddIn.comhost.dll" (
        powershell -command "$p = '%DLL_DIR%\OneNoteAddIn.comhost.dll'; $b = [System.IO.File]::ReadAllBytes($p); $peOffset = [BitConverter]::ToInt32($b, 60); $machine = [BitConverter]::ToUInt16($b, $peOffset + 4); if ($machine -eq 0x8664) { Write-Host '    comhost.dll architecture: 64-bit (x64)' } elseif ($machine -eq 0x14c) { Write-Host '    comhost.dll architecture: 32-bit (x86)' } else { Write-Host '    comhost.dll architecture: Unknown (' $machine ')' }" 2>nul
    ) else (
        echo     [SKIP] comhost.dll not found
    )
) else (
    echo     [SKIP] Build directory not determined
)
echo.

echo [9] Checking runtimeconfig.json...
if exist "%BUILD_DIR%\OneNoteAddIn.runtimeconfig.json" (
    echo     [OK] runtimeconfig.json exists
    echo     --- Content ---
    type "%BUILD_DIR%\OneNoteAddIn.runtimeconfig.json"
    echo     --- End ---
    findstr /i "WindowsDesktop" "%BUILD_DIR%\OneNoteAddIn.runtimeconfig.json" >nul
    if %errorlevel% neq 0 (
        echo     [WARN] Microsoft.WindowsDesktop.App framework NOT referenced!
        echo     This is required for Windows Forms / COM add-ins
    )
) else (
    echo     [FAIL] runtimeconfig.json MISSING
    echo     This file is required for the COM host to bootstrap .NET
)
echo.

echo [10] Checking startup log...
set LOG_DIR=%APPDATA%\OneMarkDotNet\logs
if exist "%LOG_DIR%\startup.log" (
    echo     [OK] Startup log found: %LOG_DIR%\startup.log
    echo     --- Last 15 lines ---
    powershell -command "Get-Content '%LOG_DIR%\startup.log' -Tail 15"
    echo     --- End ---
) else (
    if exist "%LOG_DIR%" (
        echo     [INFO] Log directory exists but no startup.log
        echo     This means the COM class was never instantiated
    ) else (
        echo     [FAIL] Log directory does not exist: %LOG_DIR%
        echo     This means the .NET runtime never loaded the add-in
    )
)
echo.

echo ============================================
echo  Diagnostic complete
echo ============================================
echo.
echo NEXT STEPS - Try in this order:
echo.
echo 1. RE-REGISTER (most common fix):
echo    Right-click CMD as Administrator, then:
echo    regsvr32 /u "%BUILD_DIR%\OneNoteAddIn.comhost.dll"
echo    regsvr32 "%BUILD_DIR%\OneNoteAddIn.comhost.dll"
echo    register.bat
echo.
echo 2. SELF-CONTAINED PUBLISH (eliminates .NET runtime dependency):
echo    dotnet publish src\OneNoteAddIn -c Release -r win-x64
echo    register.bat
echo.
echo 3. COMHOST TRACE (get detailed loading diagnostics):
echo    set COMHOST_TRACE=1
echo    Then restart OneNote and check DebugView output
echo.
pause
