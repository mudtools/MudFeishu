#!/usr/bin/env pwsh
# FeishuWikiManager Backend Build Script

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " FeishuWikiManager Backend Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Set-Location $PSScriptRoot\backend

Write-Host "[1/3] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Package restore failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "[2/3] Building project (Debug)..." -ForegroundColor Yellow
dotnet build --configuration Debug --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "[3/3] Building project (Release)..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Release build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Build completed successfully!" -ForegroundColor Green
Write-Host " Debug:   bin\Debug\net10.0\" -ForegroundColor Green
Write-Host " Release: bin\Release\net10.0\" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
