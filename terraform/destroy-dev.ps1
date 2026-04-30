# Development Environment Destroy Script (PowerShell)
# This script removes all development resources from Azure

Write-Host "🗑️  Starting MCP Demo Development Environment Cleanup" -ForegroundColor Yellow
Write-Host "==============================================" -ForegroundColor Yellow

# Navigate to terraform directory
Set-Location terraform

# Check if terraform is initialized
if (-not (Test-Path ".terraform")) {
    Write-Host "❌ Terraform not initialized. Nothing to destroy." -ForegroundColor Red
    exit 0
}

# Check if we have state
if (-not (Test-Path "terraform.tfstate")) {
    Write-Host "❌ No terraform state found. Nothing to destroy." -ForegroundColor Red
    exit 0
}

# Show what will be destroyed
Write-Host "📋 Resources that will be destroyed:" -ForegroundColor Yellow
terraform state list | Select-Object -First 20
Write-Host ""

# Ask for confirmation
$confirmation = Read-Host "⚠️  This will permanently delete all development resources. Are you sure? (y/N)" -ForegroundColor Red
if ($confirmation -ne "y" -and $confirmation -ne "Y") {
    Write-Host "❌ Destruction cancelled." -ForegroundColor Red
    exit 0
}

# Additional confirmation
$confirmation2 = Read-Host "⚠️  Are you absolutely sure? This action cannot be undone. (y/N)" -ForegroundColor Red
if ($confirmation2 -ne "y" -and $confirmation2 -ne "Y") {
    Write-Host "❌ Destruction cancelled." -ForegroundColor Red
    exit 0
}

# Destroy the infrastructure
Write-Host "🗑️  Destroying development environment..." -ForegroundColor Red
terraform destroy -var-file="dev.tfvars" -auto-approve

# Clean up local files
Write-Host "🧹 Cleaning up local files..." -ForegroundColor Yellow
Remove-Item -Force "dev.plan" -ErrorAction SilentlyContinue
Remove-Item -Force "dev-outputs.json" -ErrorAction SilentlyContinue
Remove-Item -Force "dev-deployment-info.txt" -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "✅ Development environment destroyed successfully!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""
Write-Host "💡 Note: If you want to redeploy later, run:" -ForegroundColor Cyan
Write-Host "   .\deploy-dev.ps1" -ForegroundColor White
Write-Host ""
