#!/bin/bash

# Development Environment Destroy Script
# This script removes all development resources from Azure

set -e

echo "🗑️  Starting MCP Demo Development Environment Cleanup"
echo "=============================================="

# Navigate to terraform directory
cd terraform

# Check if terraform is initialized
if [ ! -d ".terraform" ]; then
    echo "❌ Terraform not initialized. Nothing to destroy."
    exit 0
fi

# Check if we have state
if [ ! -f "terraform.tfstate" ]; then
    echo "❌ No terraform state found. Nothing to destroy."
    exit 0
fi

# Show what will be destroyed
echo "📋 Resources that will be destroyed:"
terraform state list | head -20
echo ""

# Ask for confirmation
read -p "⚠️  This will permanently delete all development resources. Are you sure? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Destruction cancelled."
    exit 0
fi

# Additional confirmation
read -p "⚠️  Are you absolutely sure? This action cannot be undone. (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Destruction cancelled."
    exit 0
fi

# Destroy the infrastructure
echo "🗑️  Destroying development environment..."
terraform destroy -var-file="dev.tfvars" -auto-approve

# Clean up local files
echo "🧹 Cleaning up local files..."
rm -f dev.plan
rm -f dev-outputs.json
rm -f dev-deployment-info.txt

echo ""
echo "✅ Development environment destroyed successfully!"
echo "======================================"
echo ""
echo "💡 Note: If you want to redeploy later, run:"
echo "   ./deploy-dev.sh"
echo ""
