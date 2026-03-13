@echo off
cd /d "%~dp0"
rmdir /s /q "nupkg" >NUL 2>&1
rmdir /s /q "artifacts" >NUL 2>&1
rmdir /s /q "bin" >NUL 2>&1
rmdir /s /q "NativeInvoke\obj" >NUL 2>&1
rmdir /s /q "%UserProfile%\.nuget\packages\nativeinvoke" >NUL 2>&1
rmdir /s /q "%NUGET_PACKAGES%\nativeinvoke" >NUL 2>&1
mkdir "nupkg" >NUL 2>&1
nuget update -self
nuget restore
dotnet restore --no-cache
dotnet build -c Release --no-restore --no-incremental
dotnet pack -c Release --no-build
rem pause >NUL
exit /b 0
