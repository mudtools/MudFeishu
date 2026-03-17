#!/usr/bin/env pwsh
# FeishuWikiManager Backend Run Script

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " FeishuWikiManager Backend Run Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Set-Location $PSScriptRoot\backend

$env:ASPNETCORE_ENVIRONMENT = "Development"

Write-Host "Starting backend server..." -ForegroundColor Yellow
Write-Host "Environment: $env:ASPNETCORE_ENVIRONMENT" -ForegroundColor Yellow
Write-Host "URL: http://localhost:5000" -ForegroundColor Yellow
Write-Host ""

dotnet run --configuration Debug --urls "http://localhost:5000"
