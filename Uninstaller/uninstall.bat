@echo off
title ATtiny Studio Uninstaller
setlocal enabledelayedexpansion


:: 2. Identify Application Directory
set "APP_DIR=%~dp0"
set "APP_DIR=%APP_DIR:~0,-1%"

echo ================================================
echo    ATtiny Studio - System Cleanup
echo ================================================
echo.
echo [INFO] Target Directory: %APP_DIR%

:: 3. Kill All Application Processes
echo [1/4] Terminating running application...
taskkill /F /IM "__EXE_NAME__" /T >nul 2>&1
:: Also kill any generic instances just in case
taskkill /F /IM "ATtinyStudio.exe" /T >nul 2>&1
timeout /t 2 /nobreak >nul

:: 4. Remove Registry Entries
echo [2/4] Wiping registry configuration...
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\ATtinyStudio" /f >nul 2>&1

:: 5. Remove Shortcuts
echo [3/4] Cleaning up desktop shortcuts...
powershell -NoProfile -Command "$d1 = [Environment]::GetFolderPath('Desktop'); $d2 = [Environment]::GetFolderPath('CommonDesktopDirectory'); foreach($d in $d1,$d2) { if($d) { $s = Join-Path $d 'ATtiny Studio.lnk'; if (Test-Path $s) { Remove-Item $s -Force } } }" >nul 2>&1

:: 6. Final File Deletion (Self-Destruct)
echo [4/4] Finalizing file removal...
echo.
echo ATtiny Studio has been successfully uninstalled.
echo.

:: Release the directory lock for this process
cd /d %TEMP%

:: Trigger self-deletion via PowerShell to handle folder locks and retries reliably.
:: We wait 3 seconds for this script to exit, then attempt deletion up to 10 times.
start /b "" powershell -NoProfile -Command "Start-Sleep -s 3; for($i=1; $i -le 10; $i++) { if (Test-Path '%APP_DIR%') { Remove-Item -Path '%APP_DIR%' -Recurse -Force -ErrorAction SilentlyContinue; Start-Sleep -s 1 } else { break } }"
exit
