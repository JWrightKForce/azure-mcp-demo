# Azure DevOps Assistant - MCP Demo

A comprehensive Model Context Protocol (MCP) server that enables AI assistants to manage Azure resources and DevOps workflows through natural language.

## 🎯 Demo Features

### MCP Tools
- **Resource Analyzer**: Query Azure costs, resource utilization, and recommendations
- **Security Auditor**: Scan resources for compliance, check Key Vault access
- **Cost Management**: Real-time cost analysis with monthly breakdowns
- **Resource Discovery**: Find and filter Azure resources by type
- **Dashboard Integration**: Live Azure metrics and insights

### Architecture
- **Backend**: .NET 8 ASP.NET Core with MCP SDK
- **Frontend**: React dashboard with Material-UI components
- **Security**: Microsoft Entra ID authentication
- **Data**: Real Azure subscription data via Azure SDK
- **Protocol**: JSON-RPC 2.0 over HTTP with Server-Sent Events

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- Azure subscription with appropriate permissions
- Azure CLI (for authentication)

### Setup and Configuration

#### 1. Clone and Install Dependencies
```bash
# Clone the repository
git clone <repository-url>
cd MCP

# Install frontend dependencies
cd frontend
npm install
cd ..
```

#### 2. Configure Azure Authentication
```bash
# Login to Azure
az login

# Set your subscription ID (required for MCP server)
az account set --subscription <YOUR_SUBSCRIPTION_ID>

# Verify your subscription
az account show
```

#### 3. Start the MCP Server
```bash
# Navigate to the backend directory
cd src/AzureDevOpsAssistant

# Start the MCP server with your Azure subscription ID
# Replace <YOUR_SUBSCRIPTION_ID> with your actual Azure subscription ID
$env:AZURE_SUBSCRIPTION_ID="<YOUR_SUBSCRIPTION_ID>"
dotnet run --urls "http://localhost:5001"

# Example with actual subscription ID:
$env:AZURE_SUBSCRIPTION_ID="41e0b717-e638-42b4-bc1c-b664112d357e"
dotnet run --urls "http://localhost:5001"
```

#### 4. Start the Frontend
```bash
# In a new terminal, navigate to frontend
cd frontend

# Start the React development server
npm start

# The dashboard will be available at http://localhost:5173
```

### Environment Variables

The MCP server requires the following environment variable:

| Variable | Description | Example |
|----------|-------------|---------|
| `AZURE_SUBSCRIPTION_ID` | Your Azure subscription ID | `41e0b717-e638-42b4-bc1c-b664112d357e` |

### Finding Your Azure Subscription ID

You can find your subscription ID using any of these methods:

#### Method 1: Azure CLI
```bash
az account show --query id -o tsv
```

#### Method 2: Azure Portal
1. Go to the Azure Portal
2. Navigate to **Subscriptions**
3. Copy the **Subscription ID** from the subscription details

#### Method 3: PowerShell
```powershell
Get-AzContext | Select-Object Subscription
```

### Troubleshooting

#### Common Issues

**"AZURE_SUBSCRIPTION_ID not found" Error**
- Make sure you've set the environment variable before starting the server
- Verify your subscription ID is correct and you have access to it

**Authentication Issues**
- Run `az login` to ensure you're authenticated to Azure
- Check that you have the necessary permissions for the subscription

**Port Conflicts**
- The MCP server uses port 5001 by default
- The frontend uses port 5173 by default
- Change the `--urls` parameter if you need different ports

#### Debug Mode
For debugging, you can enable verbose logging:
```bash
$env:AZURE_SUBSCRIPTION_ID="<YOUR_SUBSCRIPTION_ID>"
dotnet run --urls "http://localhost:5001" --verbosity normal
```

## 🔐 Security
- Microsoft Entra ID authentication
- RBAC with least-privilege access
- Key Vault for secrets management
- Private endpoints and VNet integration
- Comprehensive audit logging

## 📊 Demo Scenarios

### Cost Management
1. **"Show me my resource costs"** → Real-time cost analysis with monthly breakdowns
2. **"What are my most expensive resources?"** → Top cost contributors with percentages
3. **"Show me database costs"** → Filtered cost analysis by resource type

### Resource Discovery
4. **"Show me all resources"** → Complete Azure resource inventory
5. **"Find SQL databases"** → Resource type filtering
6. **"What web apps do I have?"** → Specific resource type queries

### Security Analysis
7. **"Run security audit"** → Comprehensive security vulnerability scan
8. **"What security vulnerabilities do we have?"** → Detailed security issues
9. **"Check Key Vault security"** → Specific service security analysis

### Dashboard Features
- **Live Metrics**: Real-time Azure resource counts and costs
- **Cost Trends**: 6-month historical cost analysis
- **Top Resources**: Most expensive Azure resources
- **Security Scores**: Real-time security assessment

### Example Interactions
```
User: "Show me my resource costs"
→ Displays: Total Cost: $13,819.95 USD with monthly trend chart

User: "What are my most expensive resources?"  
→ Displays: Top 10 resources with cost percentages

User: "Run security audit"
→ Displays: Security score 85% with detailed vulnerability list
```

### MCP Protocol Integration
The server implements the Model Context Protocol (MCP) with JSON-RPC 2.0:
- **Tools**: `GetResourceCosts`, `GetResourceUtilization`, `AuditResources`
- **Protocol**: HTTP POST to `/mcp` endpoint
- **Authentication**: Azure AD via DefaultAzureCredential
- **Data**: Real Azure subscription data via Azure SDK
