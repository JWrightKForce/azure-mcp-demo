namespace AzureDevOpsAssistant.Services.Interfaces;

public interface ISecurityAuditor
{
    Task<SecurityAuditResult> AuditResourcesAsync(string resourceGroupName = null);
    Task<List<Vulnerability>> ScanForVulnerabilitiesAsync(string resourceId);
    Task<List<KeyVaultAccess>> GetKeyVaultAccessAsync(string keyVaultName);
    Task<bool> CheckComplianceAsync(string resourceId, List<string> complianceStandards);
}

public class SecurityAuditResult
{
    public DateTime ScanTime { get; set; }
    public int TotalResources { get; set; }
    public int SecureResources { get; set; }
    public int ResourcesWithIssues { get; set; }
    public List<SecurityIssue> Issues { get; set; } = new();
    public double SecurityScore { get; set; }
}

public class SecurityIssue
{
    public string ResourceId { get; set; }
    public string ResourceType { get; set; }
    public string IssueType { get; set; }
    public string Description { get; set; }
    public string Severity { get; set; }
    public string Recommendation { get; set; }
    public bool Resolved { get; set; }
}

public class Vulnerability
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Severity { get; set; }
    public string ResourceId { get; set; }
    public DateTime DiscoveredDate { get; set; }
    public string RemediationSteps { get; set; }
}

public class KeyVaultAccess
{
    public string ObjectId { get; set; }
    public string DisplayName { get; set; }
    public string Permissions { get; set; }
    public DateTime AccessDate { get; set; }
    public string Operation { get; set; }
}
