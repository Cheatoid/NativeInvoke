@echo off
cd /d "%~dp0"
rmdir /s /q "bin" >NUL 2>&1
rmdir /s /q "obj" >NUL 2>&1
dotnet restore --force --no-cache
dotnet build -c LocalNuGet --no-incremental
dotnet run -c LocalNuGet --no-build
rem pause >NUL
exit /b 0
