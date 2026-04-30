# Development Environment Outputs

output "resource_group_name" {
  value = azurerm_resource_group.main.name
}

output "container_app_url" {
  value = azurerm_container_app.mcp_server.latest_revision_fqdn
}

output "container_app_name" {
  value = azurerm_container_app.mcp_server.name
}

output "key_vault_name" {
  value = var.enable_key_vault ? azurerm_key_vault.main[0].name : ""
}

output "application_insights_name" {
  value = var.enable_monitoring ? azurerm_application_insights.main[0].name : ""
}

output "managed_identity_id" {
  value = azurerm_user_assigned_identity.main.id
}

output "subscription_id" {
  value = data.azurerm_client_config.current.subscription_id
}

output "tenant_id" {
  value = data.azurerm_client_config.current.tenant_id
}

output "location" {
  value = azurerm_resource_group.main.location
}

# Development-specific information
output "cost_estimate" {
  description = "Estimated monthly cost for development environment"
  value = {
    container_app = "~$20-50/month (Consumption plan, scales to zero)"
    application_insights = var.enable_monitoring ? "~$3/month" : "$0"
    key_vault = var.enable_key_vault ? "~$13/month" : "$0"
    storage = "~$5/month (LRS)"
    total = var.enable_monitoring && var.enable_key_vault ? "~$40-70/month" : "~$25-55/month"
  }
}

output "access_information" {
  description = "Information needed to access the deployed resources"
  value = {
    mcp_server_url = azurerm_container_app.mcp_server.latest_revision_fqdn
    mcp_server_name = azurerm_container_app.mcp_server.name
    resource_group = azurerm_resource_group.main.name
    subscription_id = data.azurerm_client_config.current.subscription_id
    tenant_id = data.azurerm_client_config.current.tenant_id
    managed_identity_id = azurerm_user_assigned_identity.main.id
    key_vault_name = var.enable_key_vault ? azurerm_key_vault.main[0].name : "Not deployed"
    app_insights_name = var.enable_monitoring ? azurerm_application_insights.main[0].name : "Not deployed"
  }
}

output "next_steps" {
  description = "Next steps after deployment"
  value = [
    "1. Update your MCP server configuration with the deployed URL",
    "2. Set AZURE_SUBSCRIPTION_ID environment variable locally",
    "3. Test the MCP server connectivity",
    "4. Access the dashboard at the deployed URL",
    "5. Monitor costs in Azure Portal"
  ]
}
