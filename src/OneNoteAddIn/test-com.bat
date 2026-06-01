@echo off
echo ============================================
echo   NoteMark COM Test (net48 + RegAsm)
echo ============================================
echo.

echo === Testing COM Object Creation ===
powershell -Command ^
    "try { ^
        $obj = New-Object -ComObject 'NoteMark.AddIn'; ^
        Write-Host '[OK] COM object created'; ^
        Write-Host 'Type:' $obj.GetType().FullName; ^
        Write-Host 'Methods:'; _
        $obj.GetType().GetMethods() | ForEach-Object { Write-Host '  -' $_.Name }; ^
        [System.Runtime.InteropServices.Marshal].ReleaseComObject($obj) | Out-Null; ^
    } catch { ^
        Write-Host '[FAILED]' $_.Exception.Message; ^
    }"
echo.

echo === Checking CLSID ===
reg query "HKCR\CLSID\{B8F2E4A1-3D7C-4F9B-A5E6-8C1D2F3A4B5E}\InprocServer32" 2>nul
echo.

echo === Checking ProgID ===
reg query "HKCR\NoteMark.AddIn\CLSID" 2>nul
echo.

echo === Checking RegAsm Registration ===
reg query "HKCR\CLSID\{B8F2E4A1-3D7C-4F9B-A5E6-8C1D2F3A4B5E}\InprocServer32" /v CodeBase 2>nul
echo.

pause
