using ModelContextProtocol.Server;
using System.ComponentModel;
using AzureDevOpsAssistant.Services.Interfaces;

namespace AzureDevOpsAssistant.Tools;

[McpServerToolType]
public static class ResourceAnalyzerTool
{

    [McpServerTool, Description("Gets cost information for Azure resources with optional filtering by resource group")]
    public static async Task<string> GetResourceCosts(
        [Description("Optional resource group name to filter costs. If null, returns costs for all resources")] string? resourceGroupName,
        IServiceProvider serviceProvider)
    {
        var resourceManager = serviceProvider.GetRequiredService<IAzureResourceManager>();
        
        try
        {
            var costInfo = await resourceManager.GetResourceCostsAsync(resourceGroupName);
            
            // Generate summary string
            var summary = $"Total Cost: ${costInfo.TotalCost:F2} {costInfo.Currency}";
            
            var result = new
            {
                Summary = summary,
                TotalCost = costInfo.TotalCost,
                Currency = costInfo.Currency,
                Period = $"{costInfo.PeriodStart:yyyy-MM-dd} to {costInfo.PeriodEnd:yyyy-MM-dd}",
                ResourceCount = costInfo.ResourceCosts.Count,
                Resources = costInfo.ResourceCosts
                    .OrderByDescending(c => c.Cost)
                    .Select(c => new
                    {
                        c.ResourceName,
                        c.ResourceType,
                        c.Cost,
                        c.Currency
                    }),
                MonthlyBreakdown = costInfo.MonthlyBreakdown
            };

            return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error retrieving resource costs: {ex.Message}";
        }
    }

    [McpServerTool, Description("Gets database resources from Azure")]
    public static async Task<string> GetDatabases(
        [Description("Optional resource group name to filter databases")] string? resourceGroupName,
        IServiceProvider serviceProvider)
    {
        var resourceManager = serviceProvider.GetRequiredService<IAzureResourceManager>();
        
        try
        {
            var resources = await resourceManager.GetResourceUtilizationAsync(resourceGroupName);
            
            // Filter for SQL databases on the backend
            var databases = resources.Where(r => 
                r.Type.Contains("Sql") || r.Type.Contains("Database")
            ).ToList();
            
            var result = new
            {
                TotalResources = databases.Count,
                ResourceGroups = databases.GroupBy(r => r.ResourceGroup)
                    .Select(g => new
                    {
                        ResourceGroup = g.Key,
                        ResourceCount = g.Count(),
                        ResourceTypes = g.GroupBy(r => r.Type)
                            .Select(t => new { Type = t.Key, Count = t.Count() })
                    }),
                Resources = databases.Select(r => new
                {
                    r.Name,
                    r.Type,
                    r.Location,
                    r.ResourceGroup,
                    Tags = r.Tags.Count,
                    r.Properties
                })
            };

            return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error getting databases: {ex.Message}";
        }
    }

    [McpServerTool, Description("Analyzes resource utilization and performance metrics")]
    public static async Task<string> GetResourceUtilization(
        [Description("Optional resource group name to filter analysis")] string? resourceGroupName,
        [Description("Optional resource type filter (e.g., 'Sql', 'Web', 'Storage')")] string? resourceTypeFilter,
        IServiceProvider serviceProvider)
    {
        var resourceManager = serviceProvider.GetRequiredService<IAzureResourceManager>();
        
        try
        {
            var resources = await resourceManager.GetResourceUtilizationAsync(resourceGroupName);
            
            // Apply resource type filter if provided
            if (!string.IsNullOrEmpty(resourceTypeFilter))
            {
                resources = resources.Where(r => r.Type.Contains(resourceTypeFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            var result = new
            {
                TotalResources = resources.Count,
                ResourceGroups = resources.GroupBy(r => r.ResourceGroup)
                    .Select(g => new
                    {
                        ResourceGroup = g.Key,
                        ResourceCount = g.Count(),
                        ResourceTypes = g.GroupBy(r => r.Type)
                            .Select(t => new { Type = t.Key, Count = t.Count() })
                    }),
                Resources = resources.Select(r => new
                {
                    r.Name,
                    r.Type,
                    r.Location,
                    r.ResourceGroup,
                    Tags = r.Tags.Count,
                    r.Properties
                })
            };

            return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error analyzing resource utilization: {ex.Message}";
        }
    }

    [McpServerTool, Description("Gets cost optimization recommendations for Azure resources")]
    public static async Task<string> GetCostRecommendations(IServiceProvider serviceProvider)
    {
        var resourceManager = serviceProvider.GetRequiredService<IAzureResourceManager>();
        
        try
        {
            var recommendations = await resourceManager.GetCostRecommendationsAsync();
            
            var result = new
            {
                TotalRecommendations = recommendations.Count,
                PotentialSavings = recommendations.Sum(r => r.PotentialSavings),
                Currency = recommendations.FirstOrDefault()?.Currency ?? "USD",
                RecommendationsByPriority = recommendations
                    .GroupBy(r => r.Priority)
                    .Select(g => new
                    {
                        Priority = g.Key,
                        Count = g.Count(),
                        PotentialSavings = g.Sum(r => r.PotentialSavings),
                        Items = g.Select(r => new
                        {
                            r.Title,
                            r.Description,
                            r.Type,
                            r.PotentialSavings,
                            r.Currency
                        })
                    })
                    .OrderByDescending(g => g.Priority)
            };

            return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error retrieving cost recommendations: {ex.Message}";
        }
    }

    [McpServerTool, Description("Gets detailed information about a specific Azure resource")]
    public static async Task<string> GetResourceDetails(
        [Description("The full resource ID of the Azure resource")] string resourceId,
        IServiceProvider serviceProvider)
    {
        var resourceManager = serviceProvider.GetRequiredService<IAzureResourceManager>();
        
        try
        {
            var resource = await resourceManager.GetResourceDetailsAsync(resourceId);
            
            var result = new
            {
                resource.Id,
                resource.Name,
                resource.Type,
                resource.Location,
                resource.ResourceGroup,
                Properties = resource.Properties,
                Tags = resource.Tags,
                PropertyCount = resource.Properties.Count,
                TagCount = resource.Tags.Count
            };

            return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error retrieving resource details: {ex.Message}";
        }
    }
}
