using ModelContextProtocol.Server;
using System.ComponentModel;
using AzureDevOpsAssistant.Services.Interfaces;

namespace AzureDevOpsAssistant.Tools;

[McpServerToolType]
public static class SecurityAuditorTool
{
    [McpServerTool, Description("Performs a comprehensive security audit of Azure resources")]
    public static async Task<string> AuditResources(
        [Description("Optional resource group name to limit the audit scope")] string? resourceGroupName,
        IServiceProvider serviceProvider)
    {
        var securityAuditor = serviceProvider.GetRequiredService<ISecurityAuditor>();
        
        try
        {
            var auditResult = await securityAuditor.AuditResourcesAsync(resourceGroupName);
            
            var result = new
            {
                auditResult.ScanTime,
                auditResult.TotalResources,
                auditResult.SecureResources,
                auditResult.ResourcesWithIssues,
                auditResult.SecurityScore,
                IssuesBySeverity = auditResult.Issues
                    .GroupBy(i => i.Severity)
                    .Select(g => new
                    {
                        Severity = g.Key,
                        Count = g.Count(),
                        Issues = g.Select(i => new
                        {
                            i.ResourceId,
                            i.ResourceType,
                            i.IssueType,
                            i.Description,
                            i.Recommendation,
                            i.Resolved
                        })
                    })
                    .OrderByDescending(g => CountBySeverity(g.Severity))
            };

            return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error performing security audit: {ex.Message}";
        }
    }

    [McpServerTool, Description("Scans a specific resource for security vulnerabilities")]
    public static async Task<string> ScanForVulnerabilities(
        [Description("The resource ID to scan for vulnerabilities")] string resourceId,
        IServiceProvider serviceProvider)
    {
        var securityAuditor = serviceProvider.GetRequiredService<ISecurityAuditor>();
        
        try
        {
            var vulnerabilities = await securityAuditor.ScanForVulnerabilitiesAsync(resourceId);
            
            var result = new
            {
                ResourceId = resourceId,
                TotalVulnerabilities = vulnerabilities.Count,
                VulnerabilitiesBySeverity = vulnerabilities
                    .GroupBy(v => v.Severity)
                    .Select(g => new
                    {
                        Severity = g.Key,
                        Count = g.Count(),
                        Items = g.Select(v => new
                        {
                            v.Id,
                            v.Title,
                            v.Description,
                            v.DiscoveredDate,
                            v.RemediationSteps
                        })
                    })
                    .OrderByDescending(g => CountBySeverity(g.Severity))
            };

            return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error scanning for vulnerabilities: {ex.Message}";
        }
    }

    [McpServerTool, Description("Gets Key Vault access information and audit logs")]
    public static async Task<string> GetKeyVaultAccess(
        [Description("The name of the Key Vault to analyze")] string keyVaultName,
        IServiceProvider serviceProvider)
    {
        var securityAuditor = serviceProvider.GetRequiredService<ISecurityAuditor>();
        
        try
        {
            var accessLogs = await securityAuditor.GetKeyVaultAccessAsync(keyVaultName);
            
            var result = new
            {
                KeyVaultName = keyVaultName,
                TotalAccessEvents = accessLogs.Count,
                AccessByOperation = accessLogs
                    .GroupBy(a => a.Operation)
                    .Select(g => new
                    {
                        Operation = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(g => g.Count),
                RecentAccess = accessLogs
                    .OrderByDescending(a => a.AccessDate)
                    .Take(20)
                    .Select(a => new
                    {
                        a.DisplayName,
                        a.Permissions,
                        a.AccessDate,
                        a.Operation
                    })
            };

            return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error retrieving Key Vault access information: {ex.Message}";
        }
    }

    [McpServerTool, Description("Checks resource compliance against specified standards")]
    public static async Task<string> CheckCompliance(
        [Description("The resource ID to check for compliance")] string resourceId,
        [Description("List of compliance standards to check against (e.g., 'ISO 27001', 'SOC 2', 'HIPAA'")] List<string> complianceStandards,
        IServiceProvider serviceProvider)
    {
        var securityAuditor = serviceProvider.GetRequiredService<ISecurityAuditor>();
        
        try
        {
            var isCompliant = await securityAuditor.CheckComplianceAsync(resourceId, complianceStandards);
            
            var result = new
            {
                ResourceId = resourceId,
                CheckedStandards = complianceStandards,
                IsCompliant = isCompliant,
                CheckTime = DateTime.UtcNow,
                Recommendation = isCompliant 
                    ? "Resource meets all specified compliance standards"
                    : "Resource has compliance issues that need to be addressed"
            };

            return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error checking compliance: {ex.Message}";
        }
    }

    private static int CountBySeverity(string severity)
    {
        return severity.ToLower() switch
        {
            "critical" => 4,
            "high" => 3,
            "medium" => 2,
            "low" => 1,
            _ => 0
        };
    }
}
