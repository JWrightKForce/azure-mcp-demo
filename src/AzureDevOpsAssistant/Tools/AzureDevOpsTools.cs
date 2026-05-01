using ModelContextProtocol.Server;
using AzureDevOpsAssistant.Services.Interfaces;
using System.ComponentModel;
using System.Text.Json;

namespace AzureDevOpsAssistant.Tools;

[McpServerToolType]
public class AzureDevOpsTools
{
    private readonly IAzureResourceManager _resourceManager;
    private readonly ISecurityAuditor _securityAuditor;

    public AzureDevOpsTools(IAzureResourceManager resourceManager, ISecurityAuditor securityAuditor)
    {
        _resourceManager = resourceManager;
        _securityAuditor = securityAuditor;
    }

    [McpServerTool]
    [Description("Get cost information for Azure resources with optional filtering by resource group")]
    public async Task<object> GetResourceCosts(
        [Description("Optional resource group name to filter costs by")] string? resourceGroupName = null)
    {
        var costResult = await _resourceManager.GetResourceCostsAsync(resourceGroupName);
        
        return new 
        {
            summary = $"Total Cost: {costResult.TotalCost:C} USD for {costResult.ResourceCosts.Count} resources from {costResult.PeriodStart:yyyy-MM-dd} to {costResult.PeriodEnd:yyyy-MM-dd}",
            totalCost = costResult.TotalCost,
            currency = costResult.Currency,
            periodStart = costResult.PeriodStart,
            periodEnd = costResult.PeriodEnd,
            resources = costResult.ResourceCosts.Select(rc => new 
            {
                rc.ResourceName,
                rc.ResourceType,
                rc.Cost,
                rc.Currency
            })
        };
    }

    [McpServerTool]
    [Description("Get utilization information for Azure resources")]
    public async Task<object> GetResourceUtilization(
        [Description("Optional resource group name to filter resources by")] string? resourceGroupName = null,
        [Description("Optional resource type filter (e.g., 'Microsoft.Web/sites')")] string? resourceTypeFilter = null)
    {
        var utilResult = await _resourceManager.GetResourceUtilizationAsync(resourceGroupName);
        
        if (!string.IsNullOrEmpty(resourceTypeFilter))
        {
            utilResult = utilResult.Where(r => r.Type.Contains(resourceTypeFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        
        return new 
        {
            totalResources = utilResult.Count,
            resources = utilResult
        };
    }

    [McpServerTool]
    [Description("Perform security audit on Azure resources")]
    public async Task<object> AuditResources(
        [Description("Optional resource group name to filter audit by")] string? resourceGroupName = null)
    {
        var auditResult = await _securityAuditor.AuditResourcesAsync(resourceGroupName);
        
        return new 
        {
            auditResult.SecurityScore,
            assessment = (auditResult.SecurityScore >= 80 ? "Good" : auditResult.SecurityScore >= 60 ? "Fair" : "Poor"),
            auditResult.TotalResources,
            auditResult.ResourcesWithIssues,
            issues = auditResult.Issues
        };
    }
}
