@echo off
cd /d "%~dp0"
del /q "nupkg\*.nupkg" >NUL 2>&1
rem rmdir /s /q "nupkg" >NUL 2>&1
rmdir /s /q "artifacts" >NUL 2>&1
rmdir /s /q "bin" >NUL 2>&1
rmdir /s /q "Example\bin" >NUL 2>&1
rmdir /s /q "Example\obj" >NUL 2>&1
rmdir /s /q "NativeInvoke\obj" >NUL 2>&1
rmdir /s /q "%UserProfile%\.nuget\packages\nativeinvoke" >NUL 2>&1
rmdir /s /q "%NUGET_PACKAGES%\nativeinvoke" >NUL 2>&1
rem mkdir "nupkg" >NUL 2>&1
nuget update -self
nuget restore
dotnet restore --force --no-cache
dotnet build -c Release --no-restore --no-incremental
dotnet pack -c Release --no-build  -o nupkg
rem pause >NUL
exit /b 0
