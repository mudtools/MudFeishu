#!/usr/bin/env pwsh
# FeishuWikiManager Frontend Build Script

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " FeishuWikiManager Frontend Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Set-Location $PSScriptRoot\frontend

Write-Host "[1/3] Checking Node.js version..." -ForegroundColor Yellow
node --version
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Node.js is not installed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "[2/3] Installing dependencies..." -ForegroundColor Yellow
if (-not (Test-Path "node_modules")) {
    Write-Host "Running npm install..." -ForegroundColor Yellow
    npm install
} else {
    Write-Host "node_modules exists, skipping install." -ForegroundColor Gray
    Write-Host "Run 'npm install' manually if needed." -ForegroundColor Gray
}

Write-Host ""
Write-Host "[3/3] Building project..." -ForegroundColor Yellow
npm run build
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Build completed successfully!" -ForegroundColor Green
Write-Host " Output: dist\" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
