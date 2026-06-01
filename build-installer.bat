@echo off
setlocal

echo ============================================
echo  NoteMark Installer Build Script
echo ============================================
echo.

set PROJECT_DIR=%~dp0src\OneNoteAddIn
set PUBLISH_DIR=%PROJECT_DIR%\bin\Release\net48\publish
set INSTALLER_DIR=%~dp0installer
set ISS_FILE=%INSTALLER_DIR%\setup.iss

echo [1/3] Publishing build...
dotnet publish "%PROJECT_DIR%" -c Release
if %errorlevel% neq 0 (
    echo ERROR: Publish failed!
    pause
    exit /b 1
)
echo   Published to: %PUBLISH_DIR%
echo.

echo [2/3] Preparing installer files...
if not exist "%INSTALLER_DIR%\publish" mkdir "%INSTALLER_DIR%\publish"
xcopy /s /y /q "%PUBLISH_DIR%\*" "%INSTALLER_DIR%\publish\"
echo   Files copied.
echo.

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
    echo   RegAsm.exe "%PUBLISH_DIR%\OneNoteAddIn.dll" /codebase
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
