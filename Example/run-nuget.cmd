@echo off
cd /d "%~dp0"
rmdir /s /q "bin" >NUL 2>&1
rmdir /s /q "obj" >NUL 2>&1
nuget update -self
nuget restore ..
dotnet restore
dotnet build -c NuGet
dotnet run -c NuGet
rem pause >NUL
exit /b 0
