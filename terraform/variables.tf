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

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default = {
    Environment = "dev"
    Project     = "AzureDevOpsAssistant"
    Purpose     = "MCP Demo"
  }
}
