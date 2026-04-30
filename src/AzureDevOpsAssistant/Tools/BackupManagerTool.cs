using ModelContextProtocol.Server;
using System.ComponentModel;
using AzureDevOpsAssistant.Services.Interfaces;

namespace AzureDevOpsAssistant.Tools;

[McpServerToolType]
public static class BackupManagerTool
{
    [McpServerTool, Description("Creates a backup of an Azure resource")]
    public static async Task<string> CreateBackup(
        [Description("The resource ID to backup")] string resourceId,
        IServiceProvider serviceProvider,
        [Description("Backup type (full, incremental, differential)")] string backupType = "full",
        [Description("Optional backup location (uses default if not specified)")] string? backupLocation = null)
    {
        var backupManager = serviceProvider.GetRequiredService<IBackupManager>();
        
        try
        {
            var policy = new BackupPolicy
            {
                BackupType = backupType,
                Schedule = new BackupSchedule
                {
                    Frequency = "onetime",
                    RecurrenceInterval = TimeSpan.Zero
                },
                Retention = new BackupRetention
                {
                    DailyRetentionDays = 30
                }
            };

            var result = await backupManager.CreateBackupAsync(resourceId, policy);
            
            var response = new
            {
                result.Success,
                result.BackupId,
                result.ResourceId,
                result.ResourceName,
                result.ResourceType,
                result.Status,
                result.StartTime,
                result.EndTime,
                result.SizeInBytes,
                result.Location,
                SizeInMB = result.SizeInBytes > 0 ? result.SizeInBytes / 1024.0 / 1024.0 : 0,
                result.ErrorMessage
            };

            return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error creating backup: {ex.Message}";
        }
    }

    [McpServerTool, Description("Restores a backup to a target resource")]
    public static async Task<string> RestoreBackup(
        [Description("The backup ID to restore")] string backupId,
        [Description("The target resource ID to restore to")] string targetResourceId,
        IServiceProvider serviceProvider)
    {
        var backupManager = serviceProvider.GetRequiredService<IBackupManager>();
        
        try
        {
            var result = await backupManager.RestoreBackupAsync(backupId, targetResourceId);
            
            var response = new
            {
                result.Success,
                result.RestoreId,
                result.BackupId,
                result.TargetResourceId,
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
            return $"Error restoring backup: {ex.Message}";
        }
    }

    [McpServerTool, Description("Lists all backups for a specific resource")]
    public static async Task<string> ListBackups(
        [Description("The resource ID to list backups for")] string resourceId,
        IServiceProvider serviceProvider)
    {
        var backupManager = serviceProvider.GetRequiredService<IBackupManager>();
        
        try
        {
            var backups = await backupManager.ListBackupsAsync(resourceId);
            
            var response = new
            {
                ResourceId = resourceId,
                TotalBackups = backups.Count,
                BackupsByType = backups
                    .GroupBy(b => b.BackupType)
                    .Select(g => new
                    {
                        BackupType = g.Key,
                        Count = g.Count(),
                        TotalSize = g.Sum(b => b.SizeInBytes),
                        Items = g.OrderByDescending(b => b.CreatedTime).Select(b => new
                        {
                            b.BackupId,
                            b.CreatedTime,
                            b.ExpiryTime,
                            b.SizeInBytes,
                            b.Status,
                            SizeInMB = b.SizeInBytes > 0 ? b.SizeInBytes / 1024.0 / 1024.0 : 0
                        })
                    })
            };

            return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error listing backups: {ex.Message}";
        }
    }

    [McpServerTool, Description("Gets the status of a specific backup operation")]
    public static async Task<string> GetBackupStatus(
        [Description("The backup ID to check status for")] string backupId,
        IServiceProvider serviceProvider)
    {
        var backupManager = serviceProvider.GetRequiredService<IBackupManager>();
        
        try
        {
            var status = await backupManager.GetBackupStatusAsync(backupId);
            
            var response = new
            {
                status.BackupId,
                status.Status,
                status.StartTime,
                status.EndTime,
                status.ProgressPercentage,
                status.BytesTransferred,
                status.TotalBytes,
                status.CurrentOperation,
                EstimatedTimeRemaining = status.TotalBytes > 0 && status.BytesTransferred > 0
                    ? TimeSpan.FromSeconds((status.TotalBytes - status.BytesTransferred) / (status.BytesTransferred / (DateTime.UtcNow - status.StartTime).TotalSeconds))
                    : (TimeSpan?)null
            };

            return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error getting backup status: {ex.Message}";
        }
    }

    [McpServerTool, Description("Creates a backup policy for automated backups")]
    public static async Task<string> CreateBackupPolicy(
        [Description("The resource ID to create policy for")] string resourceId,
        [Description("Policy name")] string policyName,
        [Description("Backup frequency (daily, weekly, monthly)")] string frequency,
        IServiceProvider serviceProvider,
        [Description("Retention period in days")] int retentionDays = 30,
        [Description("Hour of day to run backup (0-23)")] int hourOfDay = 2)
    {
        var backupManager = serviceProvider.GetRequiredService<IBackupManager>();
        
        try
        {
            var policy = new BackupPolicy
            {
                Name = policyName,
                ResourceId = resourceId,
                BackupType = "full",
                IsEnabled = true,
                Schedule = new BackupSchedule
                {
                    Frequency = frequency,
                    HourOfDay = hourOfDay,
                    MinuteOfHour = 0,
                    TimeZone = "UTC"
                },
                Retention = new BackupRetention
                {
                    DailyRetentionDays = retentionDays
                }
            };

            var result = await backupManager.CreateBackupPolicyAsync(resourceId, policy);
            
            var response = new
            {
                result.Success,
                result.PolicyId,
                result.PolicyName,
                result.ResourceId,
                result.CreatedTime,
                result.ErrorMessage
            };

            return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error creating backup policy: {ex.Message}";
        }
    }

    [McpServerTool, Description("Lists all backup policies for a resource")]
    public static async Task<string> ListBackupPolicies(
        [Description("The resource ID to list policies for")] string resourceId,
        IServiceProvider serviceProvider)
    {
        var backupManager = serviceProvider.GetRequiredService<IBackupManager>();
        
        try
        {
            var policies = await backupManager.ListBackupPoliciesAsync(resourceId);
            
            var response = new
            {
                ResourceId = resourceId,
                TotalPolicies = policies.Count,
                Policies = policies.Select(p => new
                {
                    p.PolicyId,
                    p.Name,
                    p.BackupType,
                    p.IsEnabled,
                    p.Schedule.Frequency,
                    p.Schedule.HourOfDay,
                    p.Retention.DailyRetentionDays,
                    p.CreatedTime
                })
            };

            return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error listing backup policies: {ex.Message}";
        }
    }
}
