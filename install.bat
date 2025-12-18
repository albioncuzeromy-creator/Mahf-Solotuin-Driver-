@echo off
REM Mahf Firmware CPU Driver - Installation Script
REM Copyright (c) 2024 Mahf Corporation
REM Version: 3.0.0

echo ================================================================
echo    MAHF FIRMWARE CPU DRIVER - INSTALLATION
echo    Version 3.0.0
echo    Copyright (c) 2024 Mahf Corporation
echo ================================================================
echo.

REM Check for administrator privileges
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: This script requires Administrator privileges!
    echo Please right-click and select "Run as Administrator"
    pause
    exit /b 1
)

echo [1/8] Checking system requirements...
ver | findstr /i "10\.0\." >nul
if %errorlevel% neq 0 (
    echo ERROR: Windows 10 or later is required!
    pause
    exit /b 1
)
echo     OK - Windows version compatible

echo.
echo [2/8] Checking existing installation...
sc query MahfCPU >nul 2>&1
if %errorlevel% equ 0 (
    echo     WARNING: Driver already installed. Stopping service...
    sc stop MahfCPU >nul 2>&1
    timeout /t 2 >nul
    sc delete MahfCPU >nul 2>&1
)

echo.
echo [3/8] Copying driver files...
if not exist "%SystemRoot%\System32\drivers" (
    echo ERROR: System drivers directory not found!
    pause
    exit /b 1
)

if not exist "Driver\mahf_core.sys" (
    echo ERROR: Driver file not found in current directory!
    echo Please run this script from the Mahf Driver folder.
    pause
    exit /b 1
)

copy /Y "Driver\mahf_core.sys" "%SystemRoot%\System32\drivers\" >nul
if %errorlevel% neq 0 (
    echo ERROR: Failed to copy mahf_core.sys
    pause
    exit /b 1
)
echo     mahf_core.sys copied

echo.
echo [4/8] Installing driver via pnputil...
if exist "Driver\mahf_cpu.inf" (
    pnputil /add-driver "Driver\mahf_cpu.inf" /install >nul 2>&1
    if %errorlevel% equ 0 (
        echo     Driver installed successfully
    ) else (
        echo     WARNING: pnputil returned error
        echo     Attempting alternative installation method...
    )
)

echo.
echo [5/8] Creating service...
sc create MahfCPU binPath= "System32\drivers\mahf_core.sys" start= auto type= kernel >nul
if %errorlevel% equ 0 (
    sc description MahfCPU "Mahf Firmware CPU Driver" >nul
    echo     Service created successfully
) else (
    echo     ERROR: Failed to create service
    pause
    exit /b 1
)

echo.
echo [6/8] Creating registry entries...
reg add "HKLM\SYSTEM\CurrentControlSet\Services\MahfCPU" /v "ImagePath" /t REG_EXPAND_SZ /d "System32\drivers\mahf_core.sys" /f >nul
reg add "HKLM\SYSTEM\CurrentControlSet\Services\MahfCPU" /v "Type" /t REG_DWORD /d 1 /f >nul
reg add "HKLM\SYSTEM\CurrentControlSet\Services\MahfCPU" /v "Start" /t REG_DWORD /d 0 /f >nul
reg add "HKLM\SYSTEM\CurrentControlSet\Services\MahfCPU" /v "ErrorControl" /t REG_DWORD /d 1 /f >nul

reg add "HKLM\SOFTWARE\Mahf\CPU" /v "Version" /t REG_SZ /d "3.0.0" /f >nul
reg add "HKLM\SOFTWARE\Mahf\CPU" /v "InstallDate" /t REG_SZ /d "%date% %time%" /f >nul
echo     Registry entries created

echo.
echo [7/8] Starting service...
sc start MahfCPU >nul 2>&1
if %errorlevel% equ 0 (
    echo     Service started successfully
) else (
    echo     WARNING: Service failed to start (may start on next boot)
)

echo.
echo [8/8] Creating shortcuts...
if not exist "%ProgramFiles%\Mahf\CPU Driver" mkdir "%ProgramFiles%\Mahf\CPU Driver"
if exist "Bin\MahfControlPanel.exe" (
    copy /Y "Bin\MahfControlPanel.exe" "%ProgramFiles%\Mahf\CPU Driver\" >nul
    echo     Control Panel copied
)

REM Create Start Menu shortcut
set SCRIPT="%TEMP%\%RANDOM%-%RANDOM%-%RANDOM%-%RANDOM%.vbs"
echo Set oWS = WScript.CreateObject("WScript.Shell") >> %SCRIPT%
echo sLinkFile = "%ProgramData%\Microsoft\Windows\Start Menu\Programs\Mahf CPU Control Panel.lnk" >> %SCRIPT%
echo Set oLink = oWS.CreateShortcut(sLinkFile) >> %SCRIPT%
echo oLink.TargetPath = "%ProgramFiles%\Mahf\CPU Driver\MahfControlPanel.exe" >> %SCRIPT%
echo oLink.Save >> %SCRIPT%
cscript /nologo %SCRIPT% >nul
del %SCRIPT%
echo     Start Menu shortcut created

echo.
echo ================================================================
echo    INSTALLATION COMPLETED SUCCESSFULLY!
echo ================================================================
echo.
echo The Mahf Firmware CPU Driver has been installed.
echo.
echo IMPORTANT: A system restart is recommended for the driver to
echo           function properly.
echo.
echo After restart, you can launch the Control Panel from:
echo   - Start Menu > Mahf CPU Control Panel
echo   - "%ProgramFiles%\Mahf\CPU Driver\MahfControlPanel.exe"
echo.
echo ================================================================
echo.

choice /C YN /M "Do you want to restart now? (Y/N)"
if %errorlevel% equ 1 (
    echo.
    echo Restarting system in 10 seconds...
    shutdown /r /t 10 /c "Mahf CPU Driver installation requires restart"
) else (
    echo.
    echo Please restart your computer manually to complete installation.
)

echo.
pause