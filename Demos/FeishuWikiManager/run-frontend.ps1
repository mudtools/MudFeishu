#!/usr/bin/env pwsh
# FeishuWikiManager Frontend Dev Server

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " FeishuWikiManager Frontend Dev Server" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Set-Location $PSScriptRoot\frontend

Write-Host "Checking Node.js version..." -ForegroundColor Yellow
node --version
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Node.js is not installed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

if (-not (Test-Path "node_modules")) {
    Write-Host "Installing dependencies..." -ForegroundColor Yellow
    npm install
}

Write-Host ""
Write-Host "Starting development server..." -ForegroundColor Yellow
Write-Host "URL: http://localhost:5173" -ForegroundColor Yellow
Write-Host ""

npm run dev
