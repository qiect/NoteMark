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
set PUB_DIR=%~dp0bin\Debug\net8.0-windows\win-x64\publish

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
if exist "%BUILD_DIR%\OneNoteAddIn.comhost.dll" (
    echo     [OK] Build directory: %BUILD_DIR%
) else (
    echo     [INFO] Standard build dir not found, checking publish dir...
    if exist "%PUB_DIR%\OneNoteAddIn.comhost.dll" (
        echo     [OK] Publish directory: %PUB_DIR%
        set "BUILD_DIR=%PUB_DIR%"
    ) else (
        echo     [FAIL] No build output found! Run: dotnet build -c Debug
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
    for /f "tokens=3" %%v in ('reg query "%ADDIN_KEY%" /v LoadBehavior 2^>nul ^| findstr /i "LoadBehavior"') do echo     LoadBehavior = %%v
) else (
    echo     [FAIL] Add-In registry key NOT found
    echo     Run register.bat to create it
)
echo.

echo [5] Checking COM CLSID registration...
reg query "HKCR\CLSID\%CLSID%\InprocServer32" >nul 2>&1
if %errorlevel% equ 0 (
    echo     [OK] CLSID registered
    for /f "tokens=3" %%v in ('reg query "HKCR\CLSID\%CLSID%\InprocServer32" /ve 2^>nul ^| findstr /r "^[^ ]"') do echo     Server: %%v
) else (
    echo     [FAIL] CLSID NOT registered
    echo     Run: regsvr32 OneNoteAddIn.comhost.dll
)
echo.

echo [6] Checking ProgId registration...
reg query "HKCR\%PROGID%\CLSID" >nul 2>&1
if %errorlevel% equ 0 (
    echo     [OK] ProgId registered
    for /f "tokens=3" %%v in ('reg query "HKCR\%PROGID%\CLSID" /ve 2^>nul ^| findstr /r "^[^ ]"') do echo     CLSID: %%v
) else (
    echo     [FAIL] ProgId NOT registered
    echo     Run: regsvr32 OneNoteAddIn.comhost.dll
)
echo.

echo [7] Checking OneNote installation...
set ONENOTE_FOUND=0
for %%p in (
    "C:\Program Files\Microsoft Office\root\Office16\ONENOTE.EXE"
    "C:\Program Files (x86)\Microsoft Office\root\Office16\ONENOTE.EXE"
    "C:\Program Files\Microsoft Office\Office16\ONENOTE.EXE"
    "C:\Program Files (x86)\Microsoft Office\Office16\ONENOTE.EXE"
) do (
    if exist %%p (
        echo     [OK] Found: %%~p
        set ONENOTE_FOUND=1
    )
)
if %ONENOTE_FOUND% equ 0 (
    echo     [WARN] OneNote not found in standard locations
)
echo.

echo [8] Checking architecture...
if "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
    echo     [INFO] System is 64-bit
) else (
    echo     [INFO] System is 32-bit
)
echo     OneNote must match add-in architecture (AnyCPU/x64)
echo.

echo [9] Checking startup log...
set LOG_DIR=%APPDATA%\OneMarkDotNet\logs
if exist "%LOG_DIR%\startup.log" (
    echo     [OK] Startup log found: %LOG_DIR%\startup.log
    echo     --- Last 10 lines ---
    powershell -command "Get-Content '%LOG_DIR%\startup.log' -Tail 10"
    echo     --- End ---
) else (
    if exist "%LOG_DIR%" (
        echo     [INFO] Log directory exists but no startup.log
        echo     This means the COM class was never instantiated
    ) else (
        echo     [FAIL] Log directory does not exist: %LOG_DIR%
        echo     This means the .NET runtime never loaded the add-in
        echo.
        echo     Possible causes:
        echo     1. .NET 8 Desktop Runtime is not installed
        echo     2. COM host DLL is not registered (regsvr32)
        echo     3. Architecture mismatch (32-bit vs 64-bit)
        echo     4. Missing dependency DLLs
    )
)
echo.

echo ============================================
echo  Diagnostic complete
echo ============================================
echo.
echo Quick fix steps:
echo   1. Install .NET 8 Desktop Runtime (x64)
echo   2. Run: dotnet build -c Debug
echo   3. Run: regsvr32 "%BUILD_DIR%\OneNoteAddIn.comhost.dll"  (as admin)
echo   4. Run: register.bat  (as admin)
echo   5. Restart OneNote
echo   6. Check: %APPDATA%\OneMarkDotNet\logs\startup.log
echo.
pause
