# Development Environment Configuration
# This file is optimized for development/testing

environment = "dev"
location = "East US"
container_registry = "mcpdemo"

# Development-specific settings
enable_private_endpoints = false
enable_monitoring = true
enable_key_vault = true

# Cost-effective settings for development
container_app_sku = "Consumption"  # Pay-per-use, scales to zero
container_app_min_replicas = 0
container_app_max_replicas = 2

# These will be automatically populated from your Azure CLI context
# tenant_id = "your-tenant-id"
# subscription_id = "your-subscription-id"

# Development resource naming
resource_group_name = "rg-mcp-demo-dev"
container_app_name = "ca-mcp-demo-dev"
key_vault_name = "kv-mcp-demo-dev"
app_insights_name = "ai-mcp-demo-dev"

# Development tags
tags = {
  Environment = "dev"
  Project = "AzureDevOpsAssistant"
  Purpose = "MCP Demo"
  CostCenter = "Development"
  Owner = "DevTeam"
}
