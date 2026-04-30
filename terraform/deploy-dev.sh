#!/bin/bash

# Development Deployment Script for MCP Demo
# This script deploys the development environment to Azure

set -e

echo "🚀 Starting MCP Demo Development Deployment"
echo "=========================================="

# Check if Azure CLI is logged in
echo "🔐 Checking Azure CLI authentication..."
if ! az account show > /dev/null 2>&1; then
    echo "❌ Not logged in to Azure. Please run 'az login' first."
    exit 1
fi

echo "✅ Azure CLI authenticated"

# Get current subscription info
echo "📋 Getting Azure subscription information..."
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)
echo "Subscription ID: $SUBSCRIPTION_ID"
echo "Tenant ID: $TENANT_ID"

# Navigate to terraform directory
cd terraform

# Initialize Terraform
echo "🔧 Initializing Terraform..."
terraform init

# Plan the deployment
echo "📋 Planning deployment..."
terraform plan -var-file="dev.tfvars" -out="dev.plan"

# Show plan summary
echo ""
echo "📊 Deployment Plan Summary:"
echo "=========================="
echo "- Environment: dev"
echo "- Location: East US"
echo "- Container App: Consumption plan (pay-per-use)"
echo "- Monitoring: Enabled"
echo "- Key Vault: Enabled"
echo "- Private Endpoints: Disabled (cost optimization)"
echo ""

# Ask for confirmation
read -p "Do you want to proceed with the deployment? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Deployment cancelled."
    exit 0
fi

# Apply the deployment
echo "🚀 Applying deployment..."
terraform apply -var-file="dev.tfvars" -auto-approve

# Get outputs
echo "📋 Getting deployment outputs..."
echo ""
terraform output -json > dev-outputs.json

# Display key outputs
echo "✅ Deployment Complete!"
echo "======================"
echo ""
echo "🔗 MCP Server URL: $(terraform output -raw container_app_url)"
echo "📊 Resource Group: $(terraform output -raw resource_group_name)"
echo "🔑 Key Vault: $(terraform output -raw key_vault_name)"
echo "📈 Application Insights: $(terraform output -raw app_insights_name)"
echo ""
echo "💰 Cost Estimate:"
echo "$(terraform output -raw cost_estimate)"
echo ""
echo "📋 Access Information:"
echo "$(terraform output -raw access_information)"
echo ""
echo "🔧 Next Steps:"
echo "$(terraform output -raw next_steps)"
echo ""

# Save outputs to a file for easy reference
cat << EOF > dev-deployment-info.txt
========================================
MCP Demo Development Deployment Info
========================================

Deployment Date: $(date)
Azure Subscription: $SUBSCRIPTION_ID

Access Information:
- MCP Server URL: $(terraform output -raw container_app_url)
- Resource Group: $(terraform output -raw resource_group_name)
- Managed Identity ID: $(terraform output -raw managed_identity_id)

Cost Estimate:
$(terraform output -raw cost_estimate)

Next Steps:
$(terraform output -raw next_steps)

Commands:
- View logs: az containerapp logs show --resource-group $(terraform output -raw resource_group_name) --name $(terraform output -raw container_app_name)
- Restart: az containerapp restart --resource-group $(terraform output -raw resource_group_name) --name $(terraform output -raw container_app_name)
- Delete: terraform destroy -var-file="dev.tfvars"
EOF

echo "📄 Deployment info saved to: dev-deployment-info.txt"
echo ""
echo "🎉 Development environment deployed successfully!"
echo "📄 Check dev-deployment-info.txt for access details"
