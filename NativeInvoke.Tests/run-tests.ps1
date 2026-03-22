#!/usr/bin/env pwsh

Write-Host "Running NativeInvoke.Tests..." -ForegroundColor Green
Write-Host ""

# Get the script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SolutionPath = Join-Path $ScriptDir ".." "NativeInvoke.slnx"
$TestProjectPath = Join-Path $ScriptDir "NativeInvoke.Tests.csproj"

# Build the solution first
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build $SolutionPath --configuration Debug

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "Running tests..." -ForegroundColor Yellow
Write-Host ""

# Run all tests
dotnet test $TestProjectPath --configuration Debug --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Host "Some tests failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "All tests passed!" -ForegroundColor Green
Read-Host "Press Enter to exit"
