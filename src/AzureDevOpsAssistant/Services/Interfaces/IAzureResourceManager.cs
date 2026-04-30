namespace AzureDevOpsAssistant.Services.Interfaces;

public interface IAzureResourceManager
{
    Task<ResourceCostInfo> GetResourceCostsAsync(string resourceGroupName = null);
    Task<List<ResourceInfo>> GetResourceUtilizationAsync(string resourceGroupName = null);
    Task<List<Recommendation>> GetCostRecommendationsAsync();
    Task<ResourceInfo> GetResourceDetailsAsync(string resourceId);
}

public class ResourceCostInfo
{
    public double TotalCost { get; set; }
    public string Currency { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public List<ResourceCost> ResourceCosts { get; set; } = new();
    public List<MonthlyCost> MonthlyBreakdown { get; set; } = new();
}

public class MonthlyCost
{
    public string Month { get; set; }
    public double Cost { get; set; }
    public int ResourceCount { get; set; }
}

public class ResourceCost
{
    public string ResourceId { get; set; }
    public string ResourceName { get; set; }
    public string ResourceType { get; set; }
    public double Cost { get; set; }
    public string Currency { get; set; }
}

public class ResourceInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Location { get; set; }
    public string ResourceGroup { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public Dictionary<string, object> Tags { get; set; } = new();
}

public class Recommendation
{
    public string Type { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ResourceId { get; set; }
    public double PotentialSavings { get; set; }
    public string Currency { get; set; }
    public int Priority { get; set; }
}
