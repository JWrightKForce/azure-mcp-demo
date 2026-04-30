namespace AzureDevOpsAssistant.Services.Interfaces;

public interface ILogAnalyzer
{
    Task<LogQueryResult> QueryLogsAsync(string query, TimeSpan timeRange);
    Task<List<Alert>> CreateAlertAsync(string alertName, string query, AlertThreshold threshold);
    Task<List<Alert>> GetActiveAlertsAsync();
    Task<LogAnalyticsResult> AnalyzePatternsAsync(string resourceId, TimeSpan timeRange);
    Task<List<LogMetric>> GetLogMetricsAsync(string resourceId, List<string> metricNames, TimeSpan timeRange);
}

public class LogQueryResult
{
    public List<LogEntry> Entries { get; set; } = new();
    public DateTime QueryTime { get; set; }
    public TimeSpan TimeRange { get; set; }
    public int TotalRecords { get; set; }
    public string Query { get; set; }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; }
    public string Message { get; set; }
    public string ResourceId { get; set; }
    public string ResourceType { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class Alert
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Query { get; set; }
    public AlertThreshold Threshold { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? TriggeredDate { get; set; }
    public string Severity { get; set; }
}

public class AlertThreshold
{
    public string Metric { get; set; }
    public double Value { get; set; }
    public string Operator { get; set; }
    public TimeSpan EvaluationFrequency { get; set; }
}

public class LogAnalyticsResult
{
    public string ResourceId { get; set; }
    public DateTime AnalysisStart { get; set; }
    public DateTime AnalysisEnd { get; set; }
    public List<LogPattern> Patterns { get; set; } = new();
    public List<LogAnomaly> Anomalies { get; set; } = new();
    public Dictionary<string, double> Metrics { get; set; } = new();
}

public class LogPattern
{
    public string Pattern { get; set; }
    public int Frequency { get; set; }
    public double Percentage { get; set; }
    public List<DateTime> Occurrences { get; set; } = new();
    public string Severity { get; set; }
}

public class LogAnomaly
{
    public string Description { get; set; }
    public DateTime DetectedAt { get; set; }
    public double AnomalyScore { get; set; }
    public string Metric { get; set; }
    public double ExpectedValue { get; set; }
    public double ActualValue { get; set; }
}

public class LogMetric
{
    public string Name { get; set; }
    public double Value { get; set; }
    public string Unit { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Dimensions { get; set; } = new();
}
