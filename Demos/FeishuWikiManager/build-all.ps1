#!/usr/bin/env pwsh
# FeishuWikiManager Build All Script

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " FeishuWikiManager Build All Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[1/2] Building Backend..." -ForegroundColor Yellow
& "$PSScriptRoot\build-backend.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Backend build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "[2/2] Building Frontend..." -ForegroundColor Yellow
& "$PSScriptRoot\build-frontend.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Frontend build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " All builds completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
