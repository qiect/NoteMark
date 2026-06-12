@echo off
echo ============================================
echo  OneMarkDotNet Deploy and Register Script
echo ============================================
echo.

set SRC=E:\Project\NoteMark\src\OneMarkDotNet\bin\Debug
set DST=C:\Program Files\OneMarkDotNet

echo [1/4] Creating install directory...
if not exist "%DST%" mkdir "%DST%"
if not exist "%DST%\Themes" mkdir "%DST%\Themes"

echo [2/4] Copying files...
copy /Y "%SRC%\OneMarkDotNet.dll" "%DST%\"
copy /Y "%SRC%\OneMarkDotNet.dll.config" "%DST%\"
copy /Y "%SRC%\OneMarkDotNet.pdb" "%DST%\"
copy /Y "%SRC%\Markdig.dll" "%DST%\"
copy /Y "%SRC%\Newtonsoft.Json.dll" "%DST%\"
copy /Y "%SRC%\Microsoft.Web.WebView2.Core.dll" "%DST%\"
copy /Y "%SRC%\Microsoft.Web.WebView2.WinForms.dll" "%DST%\"
copy /Y "%SRC%\Microsoft.Web.WebView2.Wpf.dll" "%DST%\"
copy /Y "%SRC%\WebView2Loader.dll" "%DST%\"
copy /Y "%SRC%\Microsoft.Office.Interop.OneNote.dll" "%DST%\"
copy /Y "%SRC%\Office.dll" "%DST%\"
copy /Y "%SRC%\extensibility.dll" "%DST%\"
copy /Y "%SRC%\stdole.dll" "%DST%\"
copy /Y "%SRC%\System.Buffers.dll" "%DST%\"
copy /Y "%SRC%\System.Memory.dll" "%DST%\"
copy /Y "%SRC%\System.Numerics.Vectors.dll" "%DST%\"
copy /Y "%SRC%\System.Runtime.CompilerServices.Unsafe.dll" "%DST%\"
copy /Y "%SRC%\Themes\*.*" "%DST%\Themes\"

if exist "%SRC%\runtimes" (
    echo Copying runtimes...
    xcopy /E /Y /I "%SRC%\runtimes" "%DST%\runtimes"
)

echo.
echo [3/4] Unregistering old COM registration...
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe /unregister "%DST%\OneMarkDotNet.dll" 2>nul

echo [4/4] Registering COM from new location...
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe /codebase "%DST%\OneMarkDotNet.dll"

echo.
echo Registering OneNote Add-In registry entries...
reg add "HKCU\Software\Microsoft\Office\OneNote\AddIns\OneMarkDotNet.AddIn" /v FriendlyName /d OneMarkDotNet /f
reg add "HKCU\Software\Microsoft\Office\OneNote\AddIns\OneMarkDotNet.AddIn" /v Description /d "OneNote Markdown Extension" /f
reg add "HKCU\Software\Microsoft\Office\OneNote\AddIns\OneMarkDotNet.AddIn" /v LoadBehavior /t REG_DWORD /d 3 /f

echo.
echo Adding AppID registration...
reg add "HKCR\CLSID\{B2C3D4E5-F6A7-8901-BCDE-F12345678901}" /v AppID /d "{B2C3D4E5-F6A7-8901-BCDE-F12345678901}" /f
reg add "HKCR\AppID\{B2C3D4E5-F6A7-8901-BCDE-F12345678901}" /ve /d "OneMarkDotNet.OneMarkAddIn" /f
reg add "HKCR\AppID\{B2C3D4E5-F6A7-8901-BCDE-F12345678901}" /v DllSurrogate /d "" /f

echo.
echo ============================================
echo  Done! Please restart OneNote to test.
echo ============================================
pause
