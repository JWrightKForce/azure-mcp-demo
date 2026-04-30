# Development Environment Security Configuration
# Minimal security for development - focus on cost-effectiveness

# No private endpoints for dev (cost optimization)
# Basic network security only

# Basic Network Security Group (only if needed)
resource "azurerm_network_security_group" "main" {
  count               = var.enable_private_endpoints ? 1 : 0
  name                = "nsg-mcp-demo-dev"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  
  tags = var.tags
}

# Basic NSG rules (only if private endpoints are enabled)
resource "azurerm_network_security_rule" "allow_http" {
  count               = var.enable_private_endpoints ? 1 : 0
  name                = "allow-http"
  priority            = 100
  direction           = "Inbound"
  access              = "Allow"
  protocol            = "Tcp"
  destination_port_range = "5001"
  source_address_prefix  = "*"
  resource_group_name  = azurerm_network_security_group.main[0].name
  network_security_group_name = azurerm_network_security_group.main[0].name
}

# Role Assignment for Managed Identity
resource "azurerm_role_assignment" "contributor" {
  count                = 1
  scope                = azurerm_resource_group.main.id
  role_definition_name = "Contributor"
  principal_id         = azurerm_user_assigned_identity.main.id
}

# Lock resource group to prevent accidental deletion (optional for dev)
resource "azurerm_management_lock" "resource_group" {
  count       = 0  # Disabled for dev - enable if needed
  name       = "lock-mcp-demo-dev"
  scope      = azurerm_resource_group.main.id
  lock_level = "CanNotDelete"
  notes      = "Prevents accidental deletion of MCP demo resources"
}
