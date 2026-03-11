@echo off
cd /d "%~dp0"
rmdir /s /q "bin" >NUL 2>&1
rmdir /s /q "obj" >NUL 2>&1
dotnet restore --no-cache
dotnet build -c NuGet --no-restore --no-incremental
dotnet run -c NuGet --no-build
rem pause >NUL
exit /b 0
