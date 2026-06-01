@echo off
echo ============================================
echo   OneMark AddIn Diagnostic (net48 + RegAsm)
echo ============================================
echo.

echo === .NET Framework Version ===
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe" 2>nul | findstr /i "version"
echo.

echo === DLL Check ===
set DLL=%~dp0bin\Debug\net48\OneNoteAddIn.dll
if exist "%DLL%" (
    echo [OK] DLL found: %DLL%
    for %%A in ("%DLL%") do echo     Size: %%~zA bytes
    for %%A in ("%DLL%") do echo     Modified: %%~tA
) else (
    echo [MISSING] DLL not found at %DLL%
)
echo.

echo === COM Registration Check ===
echo Checking CLSID...
reg query "HKCR\CLSID\{B8F2E4A1-3D7C-4F9B-A5E6-8C1D2F3A4B5E}" 2>nul
if %ERRORLEVEL% equ 0 (
    echo [OK] CLSID registered
) else (
    echo [MISSING] CLSID not registered
)
echo.

echo Checking ProgID...
reg query "HKCR\OneMark.AddIn" 2>nul
if %ERRORLEVEL% equ 0 (
    echo [OK] ProgID registered
) else (
    echo [MISSING] ProgID not registered
)
echo.

echo === OneNote AddIn Registry Check ===
reg query "HKCU\Software\Microsoft\Office\OneNote\AddIns\OneMark.AddIn" 2>nul
if %ERRORLEVEL% equ 0 (
    echo [OK] OneNote AddIn registry entry found
) else (
    echo [MISSING] OneNote AddIn registry entry not found
)
echo.

echo === COM Object Test ===
echo Testing COM instantiation...
powershell -Command "$obj = New-Object -ComObject 'OneMark.AddIn'; if ($obj) { Write-Host '[OK] COM object created successfully'; Write-Host 'Type:' $obj.GetType().FullName } else { Write-Host '[FAILED] COM object creation returned null' }" 2>nul
if %ERRORLEVEL% neq 0 (
    echo [FAILED] Cannot create COM object. RegAsm registration may be needed.
)
echo.

echo === OneNote Process Check ===
tasklist /fi "imagename eq ONENOTE.EXE" 2>nul | findstr /i "ONENOTE" >nul
if %ERRORLEVEL% equ 0 (
    echo [RUNNING] OneNote is currently running
) else (
    echo [NOT RUNNING] OneNote is not running
)
echo.

echo === Diagnostic Log ===
set LOGDIR=%APPDATA%\OneMark\logs
if exist "%LOGDIR%\startup.log" (
    echo Last 20 lines of startup.log:
    powershell -Command "Get-Content '%LOGDIR%\startup.log' -Tail 20"
) else (
    echo No startup.log found at %LOGDIR%
)
echo.

echo ============================================
echo   Diagnostic Complete
echo ============================================
pause
