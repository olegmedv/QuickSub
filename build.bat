@echo off
setlocal

echo ========================================
echo QuickSub Build Wrapper
echo ========================================

if "%1"=="help" (
    echo Usage: build.bat [mode]
    echo.
    echo Modes:
    echo   build.bat          - Default compact build
    echo   build.bat safe     - Safe build without trimming
    echo   build.bat compact  - Conservative build with compression
    echo   build.bat no-trim  - Standard build without trimming
    echo   build.bat help     - Show this help
    echo.
    goto :end
)

if "%1"=="safe" (
    echo Running safe build mode...
    powershell.exe -ExecutionPolicy Bypass -File "build.ps1" -SafeMode
) else if "%1"=="compact" (
    echo Running conservative build mode...
    powershell.exe -ExecutionPolicy Bypass -File "build.ps1" -ConservativeMode
) else if "%1"=="no-trim" (
    echo Running build without trimming...
    powershell.exe -ExecutionPolicy Bypass -File "build.ps1" -NoTrimming
) else (
    echo Running default conservative build...
    powershell.exe -ExecutionPolicy Bypass -File "build.ps1" -ConservativeMode
)

:end
echo.
echo Build completed. Check output above for any errors.
pause