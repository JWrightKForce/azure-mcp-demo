# MCP Demo - Development Environment Deployment

This directory contains Terraform scripts for deploying the MCP demo to a cost-effective Azure development environment.

## 🎯 Development Environment Features

### Cost-Optimized Configuration
- **Container Apps**: Consumption plan (pay-per-use, scales to zero)
- **Monitoring**: Application Insights with Log Analytics
- **Security**: Azure Key Vault with Managed Identity
- **Networking**: Public endpoints (cost optimization for dev)
- **Est. Monthly Cost**: $25-55 (vs $100+ for production)

### Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                    Azure Development Environment                    │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │  Container Apps  │  │  Application    │  │   Key Vault    │  │
│  │  (Consumption)    │  │  Insights        │  │   (Standard)    │  │
│  │                 │  │                 │  │                 │  │
│  │  MCP Server      │  │  Monitoring      │  │  Secrets        │  │
│  │  (Scales to 0)    │  │                 │  │  Management     │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## 📋 Files Overview

| File | Purpose | Description |
|------|---------|-------------|
| `dev.tfvars` | Development configuration variables | Environment-specific settings |
| `main-dev.tf` | Main infrastructure resources | Container Apps, Key Vault, Monitoring |
| `security-dev.tf` | Security configuration | Minimal security for dev |
| `outputs-dev.tf` | Output definitions | Deployment information and costs |
| `deploy-dev.ps1` | Deployment script | PowerShell deployment automation |
| `destroy-dev.ps1` | Cleanup script | Resource destruction |
| `README-dev.md` | This file | Development deployment guide |

## 🚀 Quick Start

### Prerequisites
- Azure CLI installed and logged in
- Terraform installed
- PowerShell (Windows) or Bash (Linux/Mac)

### 1. Configure Azure Authentication
```powershell
# Login to Azure
az login

# Verify your current context
az account show
```

### 2. Deploy Development Environment
```powershell
# Navigate to terraform directory
cd terraform

# Run the deployment script
.\deploy-dev.ps1
```

### 3. Access the Deployed Resources
After deployment, you'll get:
- **MCP Server URL**: `https://<app-name>.azurecontainerapps.io`
- **Resource Group**: `rg-mcp-demo-dev`
- **Key Vault**: `kv-mcp-demo-dev-xxxxxx`
- **Application Insights**: `ai-mcp-demo-dev`

## 🔧 Configuration

### Environment Variables (`dev.tfvars`)
```hcl
environment = "dev"
location = "East US"
container_registry = "mcpdemo"

# Cost-effective settings
enable_private_endpoints = false
enable_monitoring = true
enable_key_vault = true
container_app_sku = "Consumption"
container_app_min_replicas = 0
container_app_max_replicas = 2
```

### Custom Resource Names
You can customize resource names by updating these variables:
```hcl
resource_group_name = "rg-mcp-demo-dev"
container_app_name = "ca-mcp-demo-dev"
key_vault_name = "kv-mcp-demo-dev"
app_insights_name = "ai-mcp-demo-dev"
```

## 💰 Cost Breakdown

| Resource | Monthly Cost | Notes |
|----------|-------------|-------|
| Container Apps | $20-50 | Consumption plan, scales to zero |
| Application Insights | $3 | Basic monitoring |
| Key Vault | $13 | Standard tier |
| Storage | $5 | LRS storage account |
| **Total** | **$25-55** | **Development optimized** |

## 🔐 Security Configuration

### Development Security (Minimal)
- **Managed Identity**: Azure AD authentication
- **Key Vault**: Secret management
- **RBAC**: Contributor access for resource group
- **Public Endpoints**: Cost optimization for development
- **No Private Endpoints**: Simplified networking

### Production Security (Upgrade Path)
For production, consider:
- Private endpoints
- Virtual Network integration
- Advanced RBAC
- Private DNS zones
- Azure Front Door

## 📊 Monitoring and Logging

### Enabled in Development
- **Application Insights**: Request tracking, performance monitoring
- **Log Analytics**: Centralized logging
- **Azure Monitor**: Resource health and metrics

### Viewing Logs
```powershell
# Container App logs
az containerapp logs show --resource-group rg-mcp-demo-dev --name ca-mcp-demo-dev

# Restart the app
az containerapp restart --resource-group rg-mcp-demo-dev --name ca-mcp-demo-dev
```

## 🔄 Deployment vs. Production

| Feature | Development | Production |
|--------|------------|------------|
| **Container App SKU** | Consumption | Standard |
| **Replicas** | 0-2 | 3-10 |
| **Private Endpoints** | ❌ | ✅ |
| **Monitoring** | Basic | Advanced |
| **Cost** | $25-55/month | $100+/month |
| **Scaling** | Manual | Auto |

## 🧹 Maintenance

### Daily Operations
- Monitor costs in Azure Portal
- Check Application Insights for errors
- Review Key Vault access patterns
- Update MCP server configuration as needed

### Weekly Operations
- Review resource utilization
- Update Azure SDK versions
- Check for security updates
- Backup configuration changes

### Monthly Operations
- Review cost breakdown
- Update Terraform modules
- Rotate secrets if needed
- Plan production upgrades

## 🗑️ Cleanup

### Destroy Development Environment
```powershell
cd terraform
.\destroy-dev.ps1
```

### What Gets Destroyed
- Container App and Environment
- Key Vault and secrets
- Application Insights
- Resource Group and all resources
- Managed Identity

## 🔧 Troubleshooting

### Common Issues

#### "az login: command not found"
```powershell
# Install Azure CLI
winget install Microsoft.AzureCLI
# Or download from: https://aka.ms/azure-cli
```

#### "Terraform: command not found"
```powershell
# Install Terraform
winget install hashicorp.terraform
# Or download from: https://www.terraform.io/downloads.html
```

#### "Access denied" errors
```powershell
# Check your Azure permissions
az role assignment list --assignee $(az ad signed-in-user show --query objectId -o tsv)

# Ensure you have Contributor role on the subscription
az role assignment create --assignee $(az ad signed-in-user show --query objectId -o tsv) --role "Contributor" --scope /subscriptions/<subscription-id>
```

#### Container App deployment failures
```powershell
# Check logs
az containerapp logs show --resource-group rg-mcp-demo-dev --name ca-mcp-demo-dev

# Check revision
az containerapp revision list --resource-group rg-mcp-demo-dev --name ca-mcp-demo-dev

# Restart
az containerapp restart --resource-group rg-mcp-demo-dev --name ca-mcp-demo-dev
```

## 📚 Next Steps

After successful deployment:

1. **Test MCP Server**: Access the deployed URL and verify functionality
2. **Update Configuration**: Set `AZURE_SUBSCRIPTION_ID` in your local environment
3. **Integrate with Frontend**: Update frontend to use the deployed URL
4. **Monitor Performance**: Check Application Insights for issues
5. **Scale as Needed**: Adjust replicas or upgrade to production tier

## 📞 Support

### Azure Documentation
- [Container Apps](https://docs.microsoft.com/en-us/azure/container-apps/)
- [Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [Key Vault](https://docs.microsoft.com/en-us/azure/key-vault/)
- [Terraform Azure Provider](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs)

### Community
- [Azure DevOps Assistant Repository](https://github.com/JWrightKforce/mcp-demo)
- [Terraform Documentation](https://www.terraform.io/docs/)
- [Azure Developer Forums](https://learn.microsoft.com/en-us/answers/azure/)

---

**Happy developing! 🚀**
