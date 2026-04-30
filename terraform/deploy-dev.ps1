# Development Deployment Script for MCP Demo (PowerShell)
# This script deploys the development environment to Azure

Write-Host "🚀 Starting MCP Demo Development Deployment" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green

# Check if Azure CLI is logged in
Write-Host "🔐 Checking Azure CLI authentication..." -ForegroundColor Yellow
try {
    $account = az account show | ConvertFrom-Json
    Write-Host "✅ Azure CLI authenticated" -ForegroundColor Green
} catch {
    Write-Host "❌ Not logged in to Azure. Please run 'az login' first." -ForegroundColor Red
    exit 1
}

# Get current subscription info
Write-Host "📋 Getting Azure subscription information..." -ForegroundColor Yellow
$subscriptionId = $account.id
$tenantId = $account.tenantId
Write-Host "Subscription ID: $subscriptionId" -ForegroundColor Cyan
Write-Host "Tenant ID: $tenantId" -ForegroundColor Cyan

# Navigate to terraform directory
Set-Location terraform

# Initialize Terraform
Write-Host "🔧 Initializing Terraform..." -ForegroundColor Yellow
terraform init

# Plan the deployment
Write-Host "📋 Planning deployment..." -ForegroundColor Yellow
terraform plan -var-file="dev.tfvars" -out="dev.plan"

# Show plan summary
Write-Host ""
Write-Host "📊 Deployment Plan Summary:" -ForegroundColor Cyan
Write-Host "==========================" -ForegroundColor Cyan
Write-Host "- Environment: dev" -ForegroundColor White
Write-Host "- Location: East US" -ForegroundColor White
Write-Host "- Container App: Consumption plan (pay-per-use)" -ForegroundColor White
Write-Host "- Monitoring: Enabled" -ForegroundColor White
Write-Host "- Key Vault: Enabled" -ForegroundColor White
Write-Host "- Private Endpoints: Disabled (cost optimization)" -ForegroundColor White
Write-Host ""

# Ask for confirmation
$confirmation = Read-Host "Do you want to proceed with the deployment? (y/N)" -ForegroundColor Yellow
if ($confirmation -ne "y" -and $confirmation -ne "Y") {
    Write-Host "❌ Deployment cancelled." -ForegroundColor Red
    exit 0
}

# Apply the deployment
Write-Host "🚀 Applying deployment..." -ForegroundColor Green
terraform apply -var-file="dev.tfvars" -auto-approve

# Get outputs
Write-Host "📋 Getting deployment outputs..." -ForegroundColor Yellow
Write-Host ""
$outputs = terraform output -json | ConvertFrom-Json

# Display key outputs
Write-Host "✅ Deployment Complete!" -ForegroundColor Green
Write-Host "======================" -ForegroundColor Green
Write-Host ""
Write-Host "🔗 MCP Server URL: $($outputs.container_app_url.value)" -ForegroundColor Cyan
Write-Host "📊 Resource Group: $($outputs.resource_group_name.value)" -ForegroundColor Cyan
Write-Host "🔑 Key Vault: $($outputs.key_vault_name.value)" -ForegroundColor Cyan
Write-Host "📈 Application Insights: $($outputs.application_insights_name.value)" -ForegroundColor Cyan
Write-Host ""
Write-Host "💰 Cost Estimate:" -ForegroundColor Yellow
Write-Host "$($outputs.cost_estimate.value)" -ForegroundColor White
Write-Host ""
Write-Host "📋 Access Information:" -ForegroundColor Cyan
Write-Host "$($outputs.access_information.value)" -ForegroundColor White
Write-Host ""
Write-Host "🔧 Next Steps:" -ForegroundColor Cyan
Write-Host "$($outputs.next_steps.value)" -ForegroundColor White

# Save outputs to a file for easy reference
$deploymentInfo = @"
========================================
MCP Demo Development Deployment Info
========================================

Deployment Date: $(Get-Date)
Azure Subscription: $subscriptionId

Access Information:
- MCP Server URL: $($outputs.container_app_url.value)
- Resource Group: $($outputs.resource_group_name.value)
- Managed Identity ID: $($outputs.managed_identity_id.value)

Cost Estimate:
$($outputs.cost_estimate.value)

Next Steps:
$($outputs.next_steps.value)

Commands:
- View logs: az containerapp logs show --resource-group $($outputs.resource_group_name.value) --name $($outputs.container_app_name.value)
- Restart: az containerapp restart --resource-group $($outputs.resource_group_name.value) --name $($outputs.container_app_name.value)
- Delete: terraform destroy -var-file="dev.tfvars"
"@

$deploymentInfo | Out-File -FilePath "dev-deployment-info.txt" -Encoding UTF8

Write-Host "📄 Deployment info saved to: dev-deployment-info.txt" -ForegroundColor Green
Write-Host ""
Write-Host "🎉 Development environment deployed successfully!" -ForegroundColor Green
Write-Host "📄 Check dev-deployment-info.txt for access details" -ForegroundColor Green
