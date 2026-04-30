variable "environment" {
  description = "The environment name (dev, staging, prod)"
  type        = string
  default     = "dev"
}

variable "location" {
  description = "The Azure region to deploy to"
  type        = string
  default     = "East US"
}

variable "container_registry" {
  description = "The container registry URL"
  type        = string
  default     = "mcpdemo"
}

variable "enable_private_endpoints" {
  description = "Enable private endpoints for secure access"
  type        = bool
  default     = false
}

variable "enable_monitoring" {
  description = "Enable Application Insights and Log Analytics"
  type        = bool
  default     = true
}

variable "enable_key_vault" {
  description = "Enable Azure Key Vault for secrets management"
  type        = bool
  default     = true
}

variable "container_app_sku" {
  description = "Container App SKU (Consumption for dev, Standard for prod)"
  type        = string
  default     = "Consumption"
}

variable "container_app_min_replicas" {
  description = "Minimum replicas for Container App"
  type        = number
  default     = 0
}

variable "container_app_max_replicas" {
  description = "Maximum replicas for Container App"
  type        = number
  default     = 2
}

variable "resource_group_name" {
  description = "Custom resource group name"
  type        = string
  default     = ""
}

variable "container_app_name" {
  description = "Custom Container App name"
  type        = string
  default     = ""
}

variable "key_vault_name" {
  description = "Custom Key Vault name"
  type        = string
  default     = ""
}

variable "app_insights_name" {
  description = "Custom Application Insights name"
  type        = string
  default     = ""
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default = {
    Environment = "dev"
    Project     = "AzureDevOpsAssistant"
    Purpose     = "MCP Demo"
  }
}
