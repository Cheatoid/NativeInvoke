@echo off
cd /d "%~dp0"

echo Running NativeInvoke.Tests...
echo.

REM Get the script directory
set SCRIPT_DIR=%~dp0
set SOLUTION_PATH=%SCRIPT_DIR%..\NativeInvoke.slnx
set TEST_PROJECT_PATH=%SCRIPT_DIR%NativeInvoke.Tests.csproj

REM Build the solution first
echo Building solution...
dotnet build "%SOLUTION_PATH%" --configuration Debug

if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Running tests...
echo.

REM Run all tests
dotnet test "%TEST_PROJECT_PATH%" --configuration Debug --verbosity normal

if %ERRORLEVEL% neq 0 (
    echo Some tests failed!
    pause
    exit /b 1
)

echo.
echo All tests passed!
pause
