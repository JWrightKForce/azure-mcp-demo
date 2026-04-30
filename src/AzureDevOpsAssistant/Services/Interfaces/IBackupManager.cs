namespace AzureDevOpsAssistant.Services.Interfaces;

public interface IBackupManager
{
    Task<BackupResult> CreateBackupAsync(string resourceId, BackupPolicy policy);
    Task<RestoreResult> RestoreBackupAsync(string backupId, string targetResourceId);
    Task<List<BackupInfo>> ListBackupsAsync(string resourceId);
    Task<BackupStatus> GetBackupStatusAsync(string backupId);
    Task<BackupPolicyResult> CreateBackupPolicyAsync(string resourceId, BackupPolicy policy);
    Task<List<BackupPolicy>> ListBackupPoliciesAsync(string resourceId);
}

public class BackupResult
{
    public bool Success { get; set; }
    public string BackupId { get; set; }
    public string ResourceId { get; set; }
    public string ResourceName { get; set; }
    public string ResourceType { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; }
    public long SizeInBytes { get; set; }
    public string Location { get; set; }
    public string ErrorMessage { get; set; }
}

public class RestoreResult
{
    public bool Success { get; set; }
    public string RestoreId { get; set; }
    public string BackupId { get; set; }
    public string TargetResourceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; }
    public string ErrorMessage { get; set; }
}

public class BackupInfo
{
    public string BackupId { get; set; }
    public string ResourceId { get; set; }
    public string ResourceName { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? ExpiryTime { get; set; }
    public long SizeInBytes { get; set; }
    public string Status { get; set; }
    public string BackupType { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class BackupStatus
{
    public string BackupId { get; set; }
    public string Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double ProgressPercentage { get; set; }
    public long BytesTransferred { get; set; }
    public long TotalBytes { get; set; }
    public string CurrentOperation { get; set; }
}

public class BackupPolicyResult
{
    public bool Success { get; set; }
    public string PolicyId { get; set; }
    public string ResourceId { get; set; }
    public string PolicyName { get; set; }
    public DateTime CreatedTime { get; set; }
    public string ErrorMessage { get; set; }
}

public class BackupPolicy
{
    public string PolicyId { get; set; }
    public string Name { get; set; }
    public string ResourceId { get; set; }
    public string BackupType { get; set; }
    public BackupSchedule Schedule { get; set; }
    public BackupRetention Retention { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedTime { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class BackupSchedule
{
    public string Frequency { get; set; }
    public TimeSpan RecurrenceInterval { get; set; }
    public List<DayOfWeek> DaysOfWeek { get; set; } = new();
    public int HourOfDay { get; set; }
    public int MinuteOfHour { get; set; }
    public string TimeZone { get; set; }
}

public class BackupRetention
{
    public int DailyRetentionDays { get; set; }
    public int WeeklyRetentionWeeks { get; set; }
    public int MonthlyRetentionMonths { get; set; }
    public int YearlyRetentionYears { get; set; }
}
