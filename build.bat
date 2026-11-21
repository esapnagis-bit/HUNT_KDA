@echo off
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 goto nosdk

echo [1/2] Standalone single-file build...
dotnet publish -c Release -o dist
if not errorlevel 1 goto ok_standalone

echo.
echo Standalone build failed: NuGet could not download runtime packs.
echo [2/2] Offline build. It needs the .NET 8 Desktop Runtime, which is included with the SDK.
dotnet publish -c Release -o dist-offline -p:SelfContained=false -p:PublishSingleFile=false
if errorlevel 1 goto failed

echo.
echo Done: %~dp0dist-offline\HuntKdaCalculator.exe
pause
exit /b 0

:ok_standalone
echo.
echo Done: %~dp0dist\HuntKdaCalculator.exe
pause
exit /b 0

:nosdk
echo .NET SDK 8 not found. Install it first:  winget install Microsoft.DotNet.SDK.8
pause
exit /b 1

:failed
echo Both builds failed.
pause
exit /b 1
