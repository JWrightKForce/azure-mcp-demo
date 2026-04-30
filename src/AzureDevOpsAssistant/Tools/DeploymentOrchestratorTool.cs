using ModelContextProtocol.Server;
using System.ComponentModel;
using AzureDevOpsAssistant.Services.Interfaces;

namespace AzureDevOpsAssistant.Tools;

[McpServerToolType]
public static class DeploymentOrchestratorTool
{
    [McpServerTool, Description("Deploys an ARM template to a resource group")]
    public static async Task<string> DeployArmTemplate(
        [Description("The ARM template JSON content")] string templateJson,
        [Description("The parameters JSON content for the deployment")] string parametersJson,
        [Description("The target resource group name")] string resourceGroupName,
        IServiceProvider serviceProvider)
    {
        var deploymentOrchestrator = serviceProvider.GetRequiredService<IDeploymentOrchestrator>();
        
        try
        {
            var result = await deploymentOrchestrator.DeployArmTemplateAsync(templateJson, parametersJson, resourceGroupName);
            
            var response = new
            {
                result.Success,
                result.DeploymentName,
                result.ResourceGroup,
                result.Status,
                result.StartTime,
                result.EndTime,
                DeployedResources = result.DeployedResources.Select(r => new
                {
                    r.ResourceId,
                    r.ResourceName,
                    r.ResourceType,
                    r.Status
                }),
                result.ErrorMessage,
                Errors = result.Errors.Select(e => new
                {
                    e.Code,
                    e.Message,
                    e.Target
                })
            };

            return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error deploying ARM template: {ex.Message}";
        }
    }

    [McpServerTool, Description("Deploys a Bicep file to a resource group")]
    public static async Task<string> DeployBicep(
        [Description("The path to the Bicep file")] string bicepPath,
        [Description("The parameters JSON content for the deployment")] string parametersJson,
        [Description("The target resource group name")] string resourceGroupName,
        IServiceProvider serviceProvider)
    {
        var deploymentOrchestrator = serviceProvider.GetRequiredService<IDeploymentOrchestrator>();
        
        try
        {
            var result = await deploymentOrchestrator.DeployBicepAsync(bicepPath, parametersJson, resourceGroupName);
            
            var response = new
            {
                result.Success,
                result.DeploymentName,
                result.ResourceGroup,
                result.Status,
                result.StartTime,
                result.EndTime,
                DeployedResources = result.DeployedResources.Select(r => new
                {
                    r.ResourceId,
                    r.ResourceName,
                    r.ResourceType,
                    r.Status
                }),
                result.ErrorMessage
            };

            return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error deploying Bicep file: {ex.Message}";
        }
    }

    [McpServerTool, Description("Gets the status of a specific deployment")]
    public static async Task<string> GetDeploymentStatus(
        [Description("The deployment name to check")] string deploymentName,
        [Description("The resource group containing the deployment")] string resourceGroupName,
        IServiceProvider serviceProvider)
    {
        var deploymentOrchestrator = serviceProvider.GetRequiredService<IDeploymentOrchestrator>();
        
        try
        {
            var status = await deploymentOrchestrator.GetDeploymentStatusAsync(deploymentName, resourceGroupName);
            
            var response = new
            {
                status.DeploymentName,
                status.Status,
                status.StartTime,
                status.EndTime,
                status.CorrelationId,
                status.ProgressPercentage,
                Operations = status.Operations.Select(o => new
                {
                    o.OperationId,
                    o.ResourceType,
                    o.ResourceName,
                    o.Status,
                    o.Timestamp,
                    o.OperationType
                })
            };

            return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error getting deployment status: {ex.Message}";
        }
    }

    [McpServerTool, Description("Rolls back a deployment to a previous state")]
    public static async Task<string> RollbackDeployment(
        [Description("The deployment name to rollback")] string deploymentName,
        [Description("The resource group containing the deployment")] string resourceGroupName,
        IServiceProvider serviceProvider)
    {
        var deploymentOrchestrator = serviceProvider.GetRequiredService<IDeploymentOrchestrator>();
        
        try
        {
            var result = await deploymentOrchestrator.RollbackDeploymentAsync(deploymentName, resourceGroupName);
            
            var response = new
            {
                result.Success,
                result.DeploymentName,
                result.ResourceGroup,
                result.Status,
                result.StartTime,
                result.EndTime,
                result.ErrorMessage
            };

            return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error rolling back deployment: {ex.Message}";
        }
    }

    [McpServerTool, Description("Gets the deployment history for a resource group")]
    public static async Task<string> GetDeploymentHistory(
        [Description("The resource group to get deployment history for")] string resourceGroupName,
        IServiceProvider serviceProvider)
    {
        var deploymentOrchestrator = serviceProvider.GetRequiredService<IDeploymentOrchestrator>();
        
        try
        {
            var history = await deploymentOrchestrator.GetDeploymentHistoryAsync(resourceGroupName);
            
            var response = new
            {
                ResourceGroup = resourceGroupName,
                TotalDeployments = history.Count,
                Deployments = history.OrderByDescending(d => d.Timestamp).Select(d => new
                {
                    d.DeploymentName,
                    d.Status,
                    d.Timestamp,
                    d.Template,
                    d.CorrelationId
                })
            };

            return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error getting deployment history: {ex.Message}";
        }
    }
}
