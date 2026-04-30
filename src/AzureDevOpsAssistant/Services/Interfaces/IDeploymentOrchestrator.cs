namespace AzureDevOpsAssistant.Services.Interfaces;

public interface IDeploymentOrchestrator
{
    Task<DeploymentResult> DeployArmTemplateAsync(string templateJson, string parametersJson, string resourceGroupName);
    Task<DeploymentResult> DeployBicepAsync(string bicepPath, string parametersJson, string resourceGroupName);
    Task<DeploymentStatus> GetDeploymentStatusAsync(string deploymentName, string resourceGroupName);
    Task<DeploymentResult> RollbackDeploymentAsync(string deploymentName, string resourceGroupName);
    Task<List<DeploymentHistory>> GetDeploymentHistoryAsync(string resourceGroupName);
}

public class DeploymentResult
{
    public bool Success { get; set; }
    public string DeploymentName { get; set; }
    public string ResourceGroup { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; }
    public List<DeploymentResource> DeployedResources { get; set; } = new();
    public string ErrorMessage { get; set; }
    public List<DeploymentError> Errors { get; set; } = new();
}

public class DeploymentStatus
{
    public string DeploymentName { get; set; }
    public string Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string CorrelationId { get; set; }
    public List<DeploymentOperation> Operations { get; set; } = new();
    public double ProgressPercentage { get; set; }
}

public class DeploymentResource
{
    public string ResourceId { get; set; }
    public string ResourceType { get; set; }
    public string ResourceName { get; set; }
    public string Status { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class DeploymentError
{
    public string Code { get; set; }
    public string Message { get; set; }
    public string Target { get; set; }
    public List<DeploymentErrorDetails> Details { get; set; } = new();
}

public class DeploymentErrorDetails
{
    public string Code { get; set; }
    public string Message { get; set; }
}

public class DeploymentOperation
{
    public string OperationId { get; set; }
    public string ResourceType { get; set; }
    public string ResourceName { get; set; }
    public string Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string OperationType { get; set; }
}

public class DeploymentHistory
{
    public string DeploymentName { get; set; }
    public string Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string Template { get; set; }
    public string Parameters { get; set; }
    public string CorrelationId { get; set; }
}
