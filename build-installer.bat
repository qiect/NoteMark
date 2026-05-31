@echo off
setlocal

echo ============================================
echo  OneMarkDotNet Installer Build Script
echo ============================================
echo.

set PROJECT_DIR=%~dp0src\OneNoteAddIn
set PUBLISH_DIR=%PROJECT_DIR%\bin\Release\net8.0-windows\win-x64\publish
set INSTALLER_DIR=%~dp0installer
set ISS_FILE=%INSTALLER_DIR%\setup.iss

:: Step 1: Publish self-contained
echo [1/3] Publishing self-contained build...
dotnet publish "%PROJECT_DIR%" -c Release -r win-x64
if %errorlevel% neq 0 (
    echo ERROR: Publish failed!
    pause
    exit /b 1
)
echo   Published to: %PUBLISH_DIR%
echo.

:: Step 2: Copy publish output to installer directory
echo [2/3] Preparing installer files...
if not exist "%INSTALLER_DIR%\publish" mkdir "%INSTALLER_DIR%\publish"
xcopy /s /y /q "%PUBLISH_DIR%\*" "%INSTALLER_DIR%\publish\"
echo   Files copied.
echo.

:: Step 3: Build installer with Inno Setup
echo [3/3] Building installer...
where iscc >nul 2>&1
if %errorlevel% neq 0 (
    echo WARNING: Inno Setup Compiler (iscc) not found in PATH.
    echo.
    echo Please install Inno Setup from: https://jrsoftware.org/isdl.php
    echo Then add its directory to PATH, e.g.:
    echo   set PATH=%%PATH%%;"C:\Program Files (x86)\Inno Setup 6"
    echo.
    echo Or compile manually:
    echo   iscc "%ISS_FILE%"
    echo.
    echo You can also register the add-in directly without an installer:
    echo   regsvr32 "%PUBLISH_DIR%\OneNoteAddIn.comhost.dll"
    echo   Then run register.bat
    pause
    exit /b 1
)

iscc "%ISS_FILE%"
if %errorlevel% neq 0 (
    echo ERROR: Installer build failed!
    pause
    exit /b 1
)

echo.
echo ============================================
echo  Build complete!
echo ============================================
echo.
echo Installer: %INSTALLER_DIR%\output\
echo.
pause
