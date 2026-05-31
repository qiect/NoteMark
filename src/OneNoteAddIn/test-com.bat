@echo off
setlocal

echo ============================================
echo  COM Add-In Quick Test
echo ============================================
echo.

set CLSID={B8F2E4A1-3D7C-4F9B-A5E6-8C1D2F3A4B5E}
set PROGID=OneMarkDotNet.AddIn

echo [1] Testing COM object creation via PowerShell...
echo     This tests if the COM host can load the .NET runtime.
echo.

powershell -Command ^
  "try {" ^
  "  $obj = New-Object -ComObject '%PROGID%';" ^
  "  Write-Host '  [OK] COM object created successfully!';" ^
  "  Write-Host '  Type:' $obj.GetType().FullName;" ^
  "  try { Marshal.ReleaseComObject($obj) | Out-Null } catch {}" ^
  "} catch {" ^
  "  Write-Host '  [FAIL] COM object creation failed!';" ^
  "  Write-Host '  Error:' $_.Exception.Message;" ^
  "  if ($_.Exception.InnerException) {" ^
  "    Write-Host '  Inner:' $_.Exception.InnerException.Message;" ^
  "  }" ^
  "}" 2>&1

echo.
echo [2] Checking startup log...
set LOG_DIR=%APPDATA%\OneMarkDotNet\logs
if exist "%LOG_DIR%\startup.log" (
    echo     [OK] Startup log found
    echo     --- Content ---
    type "%LOG_DIR%\startup.log"
    echo     --- End ---
) else (
    echo     [FAIL] No startup.log - .NET runtime did not load
)
echo.

echo [3] Checking CLSID InprocServer32 path...
for /f "tokens=3*" %%v in ('reg query "HKCR\CLSID\%CLSID%\InprocServer32" /ve 2^>nul ^| findstr /r "^[^ ]"') do (
    echo     Registered path: %%v
    if exist "%%v" (
        echo     [OK] File exists
    ) else (
        echo     [FAIL] File NOT found at registered path!
    )
)
echo.

pause
