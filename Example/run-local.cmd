@echo off
cd /d "%~dp0"
rmdir /s /q "bin" >NUL 2>&1
rmdir /s /q "obj" >NUL 2>&1
dotnet restore --no-cache
dotnet build -c Local --no-restore --no-incremental
dotnet run -c Local --no-build
rem pause >NUL
exit /b 0
