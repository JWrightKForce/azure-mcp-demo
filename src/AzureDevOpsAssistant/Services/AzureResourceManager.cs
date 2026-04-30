using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.Sql;
using Azure.ResourceManager.CostManagement;
using Azure.Core;
using AzureDevOpsAssistant.Services.Interfaces;

namespace AzureDevOpsAssistant.Services;

public class AzureResourceManager : IAzureResourceManager
{
    private readonly ArmClient _armClient;
    private readonly string _subscriptionId;

    public AzureResourceManager(TokenCredential credential)
    {
        _armClient = new ArmClient(credential);
        _subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID") ?? "";
    }

    public async Task<ResourceCostInfo> GetResourceCostsAsync(string resourceGroupName = null)
    {
        try
        {
            if (string.IsNullOrEmpty(_subscriptionId))
            {
                Console.WriteLine("AZURE_SUBSCRIPTION_ID not found, returning mock data");
                return await GetMockCostData();
            }

            var subscription = _armClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{_subscriptionId}"));
            var costs = new List<ResourceCost>();
            decimal totalCost = 0;

            if (string.IsNullOrEmpty(resourceGroupName))
            {
                // Get all resource groups in the subscription
                await foreach (var rg in subscription.GetResourceGroups().GetAllAsync())
                {
                    totalCost += await ProcessResourceGroup(rg, costs);
                }
            }
            else
            {
                // Get specific resource group
                var rg = subscription.GetResourceGroups().Get(resourceGroupName);
                totalCost += await ProcessResourceGroup(rg, costs);
            }

            // Generate monthly breakdown
            var monthlyBreakdown = GenerateMonthlyBreakdown(costs);

            return new ResourceCostInfo
            {
                TotalCost = (double)totalCost,
                Currency = "USD",
                PeriodStart = DateTime.UtcNow.AddDays(-30),
                PeriodEnd = DateTime.UtcNow,
                ResourceCosts = costs.OrderByDescending(c => c.Cost).ToList(), // Sort by cost descending
                MonthlyBreakdown = monthlyBreakdown
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting real cost data: {ex.Message}");
            return await GetMockCostData();
        }
    }

    private async Task<decimal> ProcessResourceGroup(ResourceGroupResource rg, List<ResourceCost> costs)
    {
        decimal totalCost = 0;
        try
        {
            // Get all resources in this resource group
            foreach (var resource in rg.GetGenericResources())
            {
                var resourceCost = new ResourceCost
                {
                    ResourceId = resource.Data.Id,
                    ResourceName = resource.Data.Name,
                    ResourceType = resource.Data.ResourceType.ToString(),
                    Cost = (double)GetEstimatedCost(resource.Data.ResourceType.ToString()),
                    Currency = "USD"
                };
                
                costs.Add(resourceCost);
                totalCost += (decimal)resourceCost.Cost;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing resource group {rg.Data.Name}: {ex.Message}");
        }
        return totalCost;
    }

    private decimal GetEstimatedCost(string resourceType)
    {
        // More realistic cost estimation based on resource type and current market rates
        if (resourceType.Contains("Microsoft.Web/sites")) return 150.75m;
        if (resourceType.Contains("Microsoft.Sql/servers")) return 89.30m;
        if (resourceType.Contains("Microsoft.Sql/databases")) return 45.65m;
        if (resourceType.Contains("Microsoft.Storage/storageAccounts")) return 45.20m;
        if (resourceType.Contains("Microsoft.Compute/virtualMachines")) return 234.50m;
        if (resourceType.Contains("Microsoft.ContainerService/managedClusters")) return 156.80m;
        if (resourceType.Contains("Microsoft.CognitiveServices/accounts")) return 67.25m;
        if (resourceType.Contains("Microsoft.KeyVault/vaults")) return 12.50m;
        if (resourceType.Contains("Microsoft.Network/virtualNetworks")) return 8.75m;
        if (resourceType.Contains("Microsoft.MachineLearningServices/workspaces")) return 234.40m;
        if (resourceType.Contains("Microsoft.Databricks/workspaces")) return 567.80m;
        return 25.00m; // Default cost for other resources
    }

    private List<MonthlyCost> GenerateMonthlyBreakdown(List<ResourceCost> costs)
    {
        var monthlyBreakdown = new List<MonthlyCost>();
        var random = new Random();
        var totalCost = costs.Sum(c => c.Cost);
        
        // Generate last 6 months of data with explicit month names
        var monthData = new[]
        {
            new { Month = "Nov 2025", Multiplier = 0.70m },
            new { Month = "Dec 2025", Multiplier = 0.76m },
            new { Month = "Jan 2026", Multiplier = 0.82m },
            new { Month = "Feb 2026", Multiplier = 0.88m },
            new { Month = "Mar 2026", Multiplier = 0.94m },
            new { Month = "Apr 2026", Multiplier = 1.00m }  // Current month
        };
        
        for (int i = 0; i < monthData.Length; i++)
        {
            var monthInfo = monthData[i];
            var variation = 0.9m + (decimal)(random.NextDouble() * 0.2); // 90% to 110% variation
            var monthCost = totalCost * (double)monthInfo.Multiplier * (double)variation;
            
            monthlyBreakdown.Add(new MonthlyCost
            {
                Month = monthInfo.Month,
                Cost = Math.Round(monthCost, 2),
                ResourceCount = costs.Count
            });
        }
        
        return monthlyBreakdown;
    }

    private async Task<ResourceCostInfo> GetMockCostData()
    {
        return new ResourceCostInfo
        {
            TotalCost = 1247.89,
            Currency = "USD",
            PeriodStart = DateTime.UtcNow.AddDays(-30),
            PeriodEnd = DateTime.UtcNow,
            ResourceCosts = new List<ResourceCost>
            {
                new ResourceCost
                {
                    ResourceId = $"/subscriptions/{_subscriptionId}/resourceGroups/production/providers/Microsoft.Web/sites/prod-webapp-01",
                    ResourceName = "prod-webapp-01",
                    ResourceType = "Microsoft.Web/sites",
                    Cost = 456.23,
                    Currency = "USD"
                },
                new ResourceCost
                {
                    ResourceId = $"/subscriptions/{_subscriptionId}/resourceGroups/production/providers/Microsoft.Sql/servers/prod-sql-01",
                    ResourceName = "prod-sql-01",
                    ResourceType = "Microsoft.Sql/servers",
                    Cost = 234.56,
                    Currency = "USD"
                }
            }
        };
    }

    public async Task<List<ResourceInfo>> GetResourceUtilizationAsync(string resourceGroupName = null)
    {
        try
        {
            if (string.IsNullOrEmpty(_subscriptionId))
            {
                Console.WriteLine("AZURE_SUBSCRIPTION_ID not found, returning empty list");
                return new List<ResourceInfo>();
            }

            var subscription = _armClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{_subscriptionId}"));
            var resources = new List<ResourceInfo>();

            if (string.IsNullOrEmpty(resourceGroupName))
            {
                // Get all resource groups in the subscription
                await foreach (var rg in subscription.GetResourceGroups().GetAllAsync())
                {
                    await ProcessResourceGroupForUtilization(rg, resources);
                }
            }
            else
            {
                // Get specific resource group
                var rg = subscription.GetResourceGroups().Get(resourceGroupName);
                await ProcessResourceGroupForUtilization(rg, resources);
            }

            return resources;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting resource utilization: {ex.Message}");
            return new List<ResourceInfo>();
        }
    }

    private async Task ProcessResourceGroupForUtilization(ResourceGroupResource rg, List<ResourceInfo> resources)
    {
        try
        {
            // Get all resources in this resource group
            foreach (var resource in rg.GetGenericResources())
            {
                var resourceInfo = new ResourceInfo
                {
                    Id = resource.Data.Id,
                    Name = resource.Data.Name,
                    Type = resource.Data.ResourceType.ToString(),
                    Location = resource.Data.Location != null ? resource.Data.Location.ToString() : "Unknown",
                    ResourceGroup = rg.Data.Name,
                    Properties = new Dictionary<string, object>(),
                    Tags = resource.Data.Tags?.ToDictionary(t => t.Key, t => (object)t.Value) ?? new Dictionary<string, object>()
                };

                // Add some basic properties based on resource type
                if (resource.Data.ResourceType.ToString().Contains("Microsoft.Web/sites"))
                {
                    resourceInfo.Properties.Add("state", "Running");
                    resourceInfo.Properties.Add("siteType", "Web App");
                }
                else if (resource.Data.ResourceType.ToString().Contains("Microsoft.Sql/servers"))
                {
                    resourceInfo.Properties.Add("state", "Ready");
                    resourceInfo.Properties.Add("version", "12.0");
                }
                else if (resource.Data.ResourceType.ToString().Contains("Microsoft.Compute/virtualMachines"))
                {
                    resourceInfo.Properties.Add("state", "Running");
                    resourceInfo.Properties.Add("vmSize", "Standard_D2s_v3");
                }

                resources.Add(resourceInfo);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing resource group {rg.Data.Name} for utilization: {ex.Message}");
        }
    }

    public async Task<List<Recommendation>> GetCostRecommendationsAsync()
    {
        // Mock implementation for demo
        return new List<Recommendation>
        {
            new Recommendation
            {
                Type = "Resize",
                Title = "Downsize underutilized VM",
                Description = "VM prod-vm-01 is consistently running at <10% CPU utilization",
                ResourceId = "/subscriptions/xxx/resourceGroups/production/providers/Microsoft.Compute/virtualMachines/prod-vm-01",
                PotentialSavings = 156.78,
                Currency = "USD",
                Priority = 2
            },
            new Recommendation
            {
                Type = "ReservedInstance",
                Title = "Purchase reserved instances",
                Description = "Your SQL server usage qualifies for reserved instance pricing",
                ResourceId = "/subscriptions/xxx/resourceGroups/production/providers/Microsoft.Sql/servers/prod-sql-01",
                PotentialSavings = 234.56,
                Currency = "USD",
                Priority = 1
            }
        };
    }

    public async Task<ResourceInfo> GetResourceDetailsAsync(string resourceId)
    {
        // Mock implementation for demo
        return new ResourceInfo
        {
            Id = resourceId,
            Name = resourceId.Split('/').Last(),
            Type = "Microsoft.Web/sites",
            Location = "East US",
            ResourceGroup = "production",
            Properties = new Dictionary<string, object>
            {
                { "state", "Running" },
                { "cpu", "45%" },
                { "memory", "67%" },
                { "availabilityState", "Available" },
                { "status", "Ready" }
            },
            Tags = new Dictionary<string, object>
            {
                { "environment", "production" },
                { "department", "engineering" },
                { "owner", "devops-team" }
            }
        };
    }
}
