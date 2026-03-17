#!/usr/bin/env pwsh
# FeishuWikiManager Development Starter

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " FeishuWikiManager Development Starter" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Starting Backend and Frontend servers..." -ForegroundColor Yellow
Write-Host ""

Start-Process pwsh -ArgumentList "-NoExit", "-File", "$PSScriptRoot\run-backend.ps1"
Start-Sleep -Seconds 3

Start-Process pwsh -ArgumentList "-NoExit", "-File", "$PSScriptRoot\run-frontend.ps1"

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Servers are starting!" -ForegroundColor Green
Write-Host " Backend:  http://localhost:5000" -ForegroundColor Green
Write-Host " Frontend: http://localhost:5173" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Press any key to exit this window..." -ForegroundColor Gray
Write-Host "(Servers will continue running in separate windows)" -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
