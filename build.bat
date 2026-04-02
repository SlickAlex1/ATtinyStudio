@echo off
title SlickAlex ATtinyStudio Build Builder
setlocal enabledelayedexpansion

:: Disable .NET CLI Telemetry
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"

:: Ensure we are in the correct directory
cd /d "%~dp0"

:: --- PATHS ---
set "COMPILER_DIR=%~dp0Compiler"
set "DOTNET=%COMPILER_DIR%\dotnet.exe"
set "TEMP_EXT=%~dp0BuildTemp"
set "ASSETS_DIR=%~dp0Assets"
set "AVR_DIR=%~dp0Avrdude"
set "DRIVERS_DIR=%~dp0Drivers"
set "META_FILE=%~dp0metadata.txt"
set "OUTPUT_DIR=%~dp0Output"
set "UNINSTALLER_SRC=%~dp0Uninstaller\uninstall.bat"

:: --- LOAD METADATA ---
if not exist "%META_FILE%" (
    echo [ERROR] metadata.txt not found!
    pause
    exit /b
)

for /f "usebackq tokens=1,2 delims==" %%a in ("%META_FILE%") do (
    if "%%a"=="VERSION" set "APP_VER=%%b"
    if "%%a"=="TITLE" set "APP_TITLE=%%b"
    if "%%a"=="AUTHOR" set "APP_AUTHOR=%%b"
    if "%%a"=="DESCRIPTION" set "APP_DESC=%%b"
    if "%%a"=="COMPANY" set "APP_COMP=%%b"
    if "%%a"=="PRODUCT" set "APP_PROD=%%b"
    if "%%a"=="COPYRIGHT" set "APP_COPY=%%b"
    if "%%a"=="EXE_NAME" set "EXE_NAME=%%b"
    if "%%a"=="LICENSE" set "APP_LICENSE=%%b"
)

:: --- GET .NET VERSION ---
set "DOTNET_VER=Unknown"
if exist "%COMPILER_DIR%\dotnet_version.txt" (
    set /p DOTNET_VER=<"%COMPILER_DIR%\dotnet_version.txt"
)

echo ================================================
echo    !APP_TITLE! BUILDER (v!APP_VER!)
echo    .NET Core SDK Version: !DOTNET_VER!
echo ================================================
echo [INFO] Target Architecture: 64-bit (win-x64)
echo [INFO] Note: Windows uses 'user32.dll' for BOTH 32-bit and 64-bit.
echo        A 64-bit process will load the 64-bit user32.dll.
echo.

:: --- VALIDATE COMPILER ---
echo [1/8] Validating .NET !DOTNET_VER! SDK...
if not exist "%DOTNET%" (
    echo [ERROR] Dotnet compiler not found at: %DOTNET%
    pause
    exit /b
)

:: --- CLEAN ---
echo [2/8] Cleaning previous build artifacts...
taskkill /F /IM "!EXE_NAME!" >nul 2>&1
if exist "bin" rd /S /Q "bin"
if exist "obj" rd /S /Q "obj"
if exist "%~dp0ATtinyStudio.csproj" del /F /Q "%~dp0ATtinyStudio.csproj"
if exist "%TEMP_EXT%" rd /S /Q "%TEMP_EXT%"
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
mkdir "%TEMP_EXT%"

:: --- GENERATE METADATA CS ---
echo [3/8] Generating AppMetadata.cs...
echo namespace AttinyStudio { > "%TEMP_EXT%\AppMetadata.cs"
echo     public static class AppMetadata { >> "%TEMP_EXT%\AppMetadata.cs"
echo         public const string Title = "!APP_TITLE!"; >> "%TEMP_EXT%\AppMetadata.cs"
echo         public const string Version = "!APP_VER!"; >> "%TEMP_EXT%\AppMetadata.cs"
echo         public const string Author = "!APP_AUTHOR!"; >> "%TEMP_EXT%\AppMetadata.cs"
echo         public const string Description = "!APP_DESC!"; >> "%TEMP_EXT%\AppMetadata.cs"
echo         public const string Copyright = "!APP_COPY!"; >> "%TEMP_EXT%\AppMetadata.cs"
echo         public const string Company = "!APP_COMP!"; >> "%TEMP_EXT%\AppMetadata.cs"
echo         public const string ExeName = "!EXE_NAME!"; >> "%TEMP_EXT%\AppMetadata.cs"
echo         public const string License = "!APP_LICENSE!"; >> "%TEMP_EXT%\AppMetadata.cs"
echo     } >> "%TEMP_EXT%\AppMetadata.cs"
echo } >> "%TEMP_EXT%\AppMetadata.cs"

:: --- ASSEMBLYINFO ---
echo [4/8] Generating AssemblyInfo.cs...
echo using System.Reflection; > "%TEMP_EXT%\AssemblyInfo.cs"
echo using System.Runtime.Versioning; >> "%TEMP_EXT%\AssemblyInfo.cs"
echo [assembly: AssemblyTitle("!APP_TITLE!")] >> "%TEMP_EXT%\AssemblyInfo.cs"
echo [assembly: AssemblyDescription("!APP_DESC!")] >> "%TEMP_EXT%\AssemblyInfo.cs"
echo [assembly: AssemblyCompany("!APP_COMP!")] >> "%TEMP_EXT%\AssemblyInfo.cs"
echo [assembly: AssemblyProduct("!APP_PROD!")] >> "%TEMP_EXT%\AssemblyInfo.cs"
echo [assembly: AssemblyCopyright("!APP_COPY!")] >> "%TEMP_EXT%\AssemblyInfo.cs"
echo [assembly: AssemblyVersion("!APP_VER!")] >> "%TEMP_EXT%\AssemblyInfo.cs"
echo [assembly: AssemblyFileVersion("!APP_VER!")] >> "%TEMP_EXT%\AssemblyInfo.cs"
echo [assembly: SupportedOSPlatform("windows")] >> "%TEMP_EXT%\AssemblyInfo.cs"

:: --- RESOURCES ---
echo [5/8] Preparing embedded resources...
set "ZIP_FILE="
for %%f in ("%AVR_DIR%\*.zip") do set "ZIP_FILE=%%f"
if "!ZIP_FILE!"=="" (echo [ERROR] Avrdude ZIP missing! & pause & exit /b)
for /f "tokens=2 delims=-v" %%a in ("!ZIP_FILE!") do set "AVR_VER=%%a"
if "!AVR_VER!"=="" set "AVR_VER=8.1"
echo !AVR_VER! > "%TEMP_EXT%\avr_version.txt"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Expand-Archive -Path '!ZIP_FILE!' -DestinationPath '%TEMP_EXT%' -Force"

if exist "%ASSETS_DIR%\" (
    for %%f in ("%ASSETS_DIR%\*.*") do (
        if not "%%~nxf"=="icon.ico" copy /Y "%%f" "%TEMP_EXT%\" >nul
    )
)
if exist "%DRIVERS_DIR%\" xcopy "%DRIVERS_DIR%" "%TEMP_EXT%\Drivers\" /E /I /Y >nul

:: Copy Uninstaller to temp and replace placeholder
if exist "%UNINSTALLER_SRC%" (
    powershell -NoProfile -Command "(Get-Content '%UNINSTALLER_SRC%') -replace '__EXE_NAME__', '!EXE_NAME!' | Set-Content '%TEMP_EXT%\uninstall.bat'" >nul
    echo [OK] Uninstaller updated with EXE_NAME and staged for embedding.
)

:: --- GENERATE CSPROJ (FORCE 64-BIT) ---
echo [6/8] Generating project file (win-x64)...
set "ICON_LINE="
if exist "%ASSETS_DIR%\icon.ico" set "ICON_LINE=<ApplicationIcon>..\Assets\icon.ico</ApplicationIcon>"
set "PROJ_FILE=%TEMP_EXT%\ATtinyStudio.csproj"

:: Get assembly name from EXE_NAME (strip .exe)
set "ASM_NAME=!EXE_NAME:.exe=!"

echo ^<Project Sdk="Microsoft.NET.Sdk"^> > "%PROJ_FILE%"
echo   ^<PropertyGroup^> >> "%PROJ_FILE%"
echo     ^<OutputType^>WinExe^</OutputType^> >> "%PROJ_FILE%"
echo     ^<TargetFramework^>net10.0-windows^</TargetFramework^> >> "%PROJ_FILE%"
echo     ^<UseWindowsForms^>true^</UseWindowsForms^> >> "%PROJ_FILE%"
echo     ^<ApplicationManifest^>..\App.manifest^</ApplicationManifest^> >> "%PROJ_FILE%"
echo     ^<AssemblyName^>!ASM_NAME!^</AssemblyName^> >> "%PROJ_FILE%"
echo     ^<GenerateAssemblyInfo^>false^</GenerateAssemblyInfo^> >> "%PROJ_FILE%"
if defined ICON_LINE echo     !ICON_LINE! >> "%PROJ_FILE%"
echo     ^<PublishSingleFile^>true^</PublishSingleFile^> >> "%PROJ_FILE%"
echo     ^<SelfContained^>true^</SelfContained^> >> "%PROJ_FILE%"
echo     ^<RuntimeIdentifier^>win-x64^</RuntimeIdentifier^> >> "%PROJ_FILE%"
echo     ^<IncludeNativeLibrariesForSelfExtract^>true^</IncludeNativeLibrariesForSelfExtract^> >> "%PROJ_FILE%"
echo     ^<PublishReadyToRun^>true^</PublishReadyToRun^> >> "%PROJ_FILE%"
echo     ^<EnableCompressionInSingleFile^>true^</EnableCompressionInSingleFile^> >> "%PROJ_FILE%"
echo     ^<SupportedOSPlatformVersion^>7.0^</SupportedOSPlatformVersion^> >> "%PROJ_FILE%"
echo     ^<SatelliteResourceLanguages^>en^</SatelliteResourceLanguages^> >> "%PROJ_FILE%"
echo     ^<DebugType^>none^</DebugType^> >> "%PROJ_FILE%"
echo     ^<DebugSymbols^>false^</DebugSymbols^> >> "%PROJ_FILE%"
echo     ^<OptimizationPreference^>Size^</OptimizationPreference^> >> "%PROJ_FILE%"
echo     ^<DebuggerSupport^>false^</DebuggerSupport^> >> "%PROJ_FILE%"
echo     ^<EnableUnsafeBinaryFormatterSerialization^>false^</EnableUnsafeBinaryFormatterSerialization^> >> "%PROJ_FILE%"
echo     ^<EventSourceSupport^>false^</EventSourceSupport^> >> "%PROJ_FILE%"
echo     ^<HttpActivityPropagationSupport^>false^</HttpActivityPropagationSupport^> >> "%PROJ_FILE%"
echo     ^<MetadataUpdaterSupport^>false^</MetadataUpdaterSupport^> >> "%PROJ_FILE%"
echo     ^<StackTraceSupport^>false^</StackTraceSupport^> >> "%PROJ_FILE%"
echo   ^</PropertyGroup^> >> "%PROJ_FILE%"
echo   ^<ItemGroup^> >> "%PROJ_FILE%"
echo     ^<PackageReference Include="System.IO.Ports" Version="9.0.0" /^> >> "%PROJ_FILE%"
echo   ^</ItemGroup^> >> "%PROJ_FILE%"
echo   ^<ItemGroup^> >> "%PROJ_FILE%"
echo     ^<Compile Include="..\*.cs" /^> >> "%PROJ_FILE%"
echo   ^</ItemGroup^> >> "%PROJ_FILE%"
echo   ^<ItemGroup^> >> "%PROJ_FILE%"
echo     ^<EmbeddedResource Include="**\*" Exclude="*.csproj;AssemblyInfo.cs;AppMetadata.cs;bin\**\*;obj\**\*"^> >> "%PROJ_FILE%"
echo       ^<LogicalName^>%%(RecursiveDir)%%(Filename)%%(Extension)^</LogicalName^> >> "%PROJ_FILE%"
echo     ^</EmbeddedResource^> >> "%PROJ_FILE%"
echo   ^</ItemGroup^> >> "%PROJ_FILE%"
echo ^</Project^> >> "%PROJ_FILE%"

:: --- COMPILE ---
echo [7/8] Compiling !APP_TITLE! (Optimized size)...
"%DOTNET%" publish "%PROJ_FILE%" -c Release -o "%OUTPUT_DIR%" /p:Version=%APP_VER%

if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Compilation failed.
    pause
    exit /b
)

:: --- POST-BUILD: CLEANUP ---
echo [8/8] Finalizing: Cleaning up and verifying output...

:: --- CLEANUP ---
echo Cleaning up temporary files...
if exist "%TEMP_EXT%" rd /S /Q "%TEMP_EXT%"
if exist "bin" rd /S /Q "bin"
if exist "obj" rd /S /Q "obj"

echo.
echo ================================================
echo Build SUCCESS! (Standalone EXE Generated)
echo EXE Location: %OUTPUT_DIR%\!EXE_NAME!
echo ================================================
pause