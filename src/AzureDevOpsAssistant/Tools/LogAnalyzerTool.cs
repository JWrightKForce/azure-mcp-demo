using ModelContextProtocol.Server;
using System.ComponentModel;
using AzureDevOpsAssistant.Services.Interfaces;

namespace AzureDevOpsAssistant.Tools;

[McpServerToolType]
public static class LogAnalyzerTool
{
    [McpServerTool, Description("Queries Azure Monitor logs with a custom KQL query")]
    public static async Task<string> QueryLogs(
        [Description("The KQL query to execute")] string query,
        IServiceProvider serviceProvider,
        [Description("Time range in hours for the query (default: 24)")] int timeRangeHours = 24)
    {
        var logAnalyzer = serviceProvider.GetRequiredService<ILogAnalyzer>();
        
        try
        {
            var timeRange = TimeSpan.FromHours(timeRangeHours);
            var result = await logAnalyzer.QueryLogsAsync(query, timeRange);
            
            var response = new
            {
                result.Query,
                result.QueryTime,
                result.TimeRange,
                result.TotalRecords,
                Entries = result.Entries.Take(100).Select(e => new
                {
                    e.Timestamp,
                    e.Level,
                    e.Message,
                    e.ResourceId,
                    e.ResourceType,
                    Properties = e.Properties.Take(10) // Limit properties for readability
                })
            };

            return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error querying logs: {ex.Message}";
        }
    }

    [McpServerTool, Description("Creates an alert based on a log query")]
    public static async Task<string> CreateAlert(
        [Description("The name for the alert")] string alertName,
        [Description("The KQL query to trigger the alert")] string query,
        IServiceProvider serviceProvider,
        [Description("The metric to monitor")] string metric,
        [Description("The threshold value")] double thresholdValue,
        [Description("The comparison operator (gt, lt, eq)")] string operatorType = "gt",
        [Description("Evaluation frequency in minutes")] int evaluationFrequencyMinutes = 5)
    {
        var logAnalyzer = serviceProvider.GetRequiredService<ILogAnalyzer>();
        
        try
        {
            var threshold = new AlertThreshold
            {
                Metric = metric,
                Value = thresholdValue,
                Operator = operatorType,
                EvaluationFrequency = TimeSpan.FromMinutes(evaluationFrequencyMinutes)
            };

            var alerts = await logAnalyzer.CreateAlertAsync(alertName, query, threshold);
            
            var response = new
            {
                Success = alerts.Any(),
                AlertName = alertName,
                CreatedAlerts = alerts.Select(a => new
                {
                    a.Id,
                    a.Name,
                    a.Description,
                    a.Severity,
                    a.IsActive,
                    a.CreatedDate
                })
            };

            return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error creating alert: {ex.Message}";
        }
    }

    [McpServerTool, Description("Gets all active alerts")]
    public static async Task<string> GetActiveAlerts(IServiceProvider serviceProvider)
    {
        var logAnalyzer = serviceProvider.GetRequiredService<ILogAnalyzer>();
        
        try
        {
            var alerts = await logAnalyzer.GetActiveAlertsAsync();
            
            var response = new
            {
                TotalActiveAlerts = alerts.Count(a => a.IsActive),
                AlertsBySeverity = alerts
                    .GroupBy(a => a.Severity)
                    .Select(g => new
                    {
                        Severity = g.Key,
                        Count = g.Count(),
                        Alerts = g.Select(a => new
                        {
                            a.Id,
                            a.Name,
                            a.Description,
                            a.IsActive,
                            a.CreatedDate,
                            a.TriggeredDate
                        })
                    })
                    .OrderByDescending(g => g.Count)
            };

            return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error retrieving active alerts: {ex.Message}";
        }
    }

    [McpServerTool, Description("Analyzes log patterns and anomalies for a specific resource")]
    public static async Task<string> AnalyzePatterns(
        [Description("The resource ID to analyze")] string resourceId,
        IServiceProvider serviceProvider,
        [Description("Time range in hours for analysis (default: 24)")] int timeRangeHours = 24)
    {
        var logAnalyzer = serviceProvider.GetRequiredService<ILogAnalyzer>();
        
        try
        {
            var timeRange = TimeSpan.FromHours(timeRangeHours);
            var result = await logAnalyzer.AnalyzePatternsAsync(resourceId, timeRange);
            
            var response = new
            {
                result.ResourceId,
                result.AnalysisStart,
                result.AnalysisEnd,
                result.Metrics,
                Patterns = result.Patterns.Select(p => new
                {
                    p.Pattern,
                    p.Frequency,
                    p.Percentage,
                    p.Severity,
                    OccurrenceCount = p.Occurrences.Count
                }),
                Anomalies = result.Anomalies.Select(a => new
                {
                    a.Description,
                    a.DetectedAt,
                    a.AnomalyScore,
                    a.Metric,
                    a.ExpectedValue,
                    a.ActualValue
                })
            };

            return System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }
        catch (Exception ex)
        {
            return $"Error analyzing patterns: {ex.Message}";
        }
    }

    [McpServerTool, Description("Gets specific log metrics for a resource")]
    public static async Task<string> GetLogMetrics(
        [Description("The resource ID to get metrics for")] string resourceId,
        [Description("List of metric names to retrieve")] List<string> metricNames,
        IServiceProvider serviceProvider,
        [Description("Time range in hours (default: 24)")] int timeRangeHours = 24)
    {
        var logAnalyzer = serviceProvider.GetRequiredService<ILogAnalyzer>();
        
        try
        {
            var timeRange = TimeSpan.FromHours(timeRangeHours);
            var metrics = await logAnalyzer.GetLogMetricsAsync(resourceId, metricNames, timeRange);
            
            var response = new
            {
                ResourceId = resourceId,
                TimeRangeHours = timeRangeHours,
                Metrics = metrics.GroupBy(m => m.Name)
                    .Select(g => new
                    {
                        MetricName = g.Key,
                        Values = g.Select(m => new
                        {
                            m.Value,
                            m.Unit,
                            m.Timestamp,
                            Dimensions = m.Dimensions
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
            return $"Error retrieving log metrics: {ex.Message}";
        }
    }
}
