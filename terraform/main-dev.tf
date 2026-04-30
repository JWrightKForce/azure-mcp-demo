# Development-specific Terraform configuration
# This file is optimized for development environments

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}
}

# Data source for current Azure context
data "azurerm_client_config" "current" {}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name != "" ? var.resource_group_name : "rg-mcp-demo-dev"
  location = var.location
  
  tags = var.tags
}

# Container Apps Environment (Consumption plan for dev)
resource "azurerm_container_app_environment" "main" {
  name                = var.container_app_name != "" ? var.container_app_name : "cae-mcp-demo-dev"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  
  tags = var.tags
}

# Application Insights (only if monitoring is enabled)
resource "azurerm_application_insights" "main" {
  count               = var.enable_monitoring ? 1 : 0
  name                = var.app_insights_name != "" ? var.app_insights_name : "ai-mcp-demo-dev"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  application_type    = "web"
  
  tags = var.tags
}

# Log Analytics Workspace (only if monitoring is enabled)
resource "azurerm_log_analytics_workspace" "main" {
  count               = var.enable_monitoring ? 1 : 0
  name                = "law-mcp-demo-dev"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  
  tags = var.tags
}

# Connect Application Insights to Log Analytics (only if monitoring is enabled)
resource "azurerm_application_insights_analytics_workspace" "main" {
  count               = var.enable_monitoring ? 1 : 0
  workspace_id        = azurerm_log_analytics_workspace.main[0].id
  application_insights_id = azurerm_application_insights.main[0].id
}

# Key Vault (only if enabled)
resource "azurerm_key_vault" "main" {
  count                        = var.enable_key_vault ? 1 : 0
  name                        = var.key_vault_name != "" ? var.key_vault_name : "kv-mcp-demo-dev"
  location                    = azurerm_resource_group.main.location
  resource_group_name         = azurerm_resource_group.main.name
  enabled_for_disk_encryption = true
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  soft_delete_retention_days  = 7
  purge_protection_enabled    = false

  sku_name = "standard"

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    secret_permissions = [
      "Get",
      "List",
      "Set",
      "Delete",
      "Recover"
    ]
  }

  tags = var.tags
}

# User Assigned Managed Identity
resource "azurerm_user_assigned_identity" "main" {
  name                = "id-mcp-demo-dev"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  
  tags = var.tags
}

# Container App for MCP Server (Consumption plan for dev)
resource "azurerm_container_app" "mcp_server" {
  name                         = "mcp-server-dev"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.main.id]
  }

  template {
    min_replicas = var.container_app_min_replicas
    max_replicas = var.container_app_max_replicas
    
    containers {
      name = "mcp-server"
      image = "mcpdemo/mcp-server:latest"
      
      resources {
        cpu    = jsonencode({
          cores = 0.5
        })
        memory = "1Gi"
      }
      
      env {
        name  = "AZURE_SUBSCRIPTION_ID"
        value = data.azurerm_client_config.current.subscription_id
      }
      
      env {
        name  = "ENVIRONMENT"
        value = var.environment
      }
    }
  }

  ingress {
    external_enabled = true
    target_port     = 5001
    
    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  tags = var.tags
}

# Grant Managed Identity access to Key Vault (only if Key Vault is enabled)
resource "azurerm_key_vault_access_policy" "main" {
  count        = var.enable_key_vault ? 1 : 0
  key_vault_id = azurerm_key_vault.main[0].id

  tenant_id = data.azurerm_client_config.current.tenant_id
  object_id = azurerm_user_assigned_identity.main.id

  secret_permissions = [
    "Get",
    "List",
    "Set",
    "Delete",
  ]
}

# Random string for unique naming
resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
  lower   = true
  numeric = true
}
