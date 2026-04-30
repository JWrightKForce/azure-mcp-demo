extern alias AzureIdentity;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Azure.Core;
using AzureDevOpsAssistant.Services;
using AzureDevOpsAssistant.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register Azure services
builder.Services.AddSingleton<TokenCredential>(sp => 
{
    return new AzureIdentity::Azure.Identity.DefaultAzureCredential();
});

// Register custom MCP server
builder.Services.AddSingleton<CustomMcpServer>();

// Register application services
builder.Services.AddSingleton<IAzureResourceManager, AzureResourceManager>();
builder.Services.AddSingleton<ISecurityAuditor, SecurityAuditor>();
builder.Services.AddSingleton<IDeploymentOrchestrator, DeploymentOrchestrator>();
builder.Services.AddSingleton<ILogAnalyzer, LogAnalyzer>();
builder.Services.AddSingleton<IBackupManager, BackupManager>();

var app = builder.Build();

// Add CORS middleware
app.UseCors();

// Health check endpoint
app.MapGet("/health", () => Results.Ok("healthy"));

// Custom MCP endpoint
app.MapPost("/mcp", async (HttpContext context, CustomMcpServer mcpServer) => {
    await mcpServer.HandleRequest(context);
});

app.Run();

public class ToolRequest
{
    public string Command { get; set; }
}

public class CustomMcpServer
{
    private readonly IAzureResourceManager _resourceManager;
    private readonly ISecurityAuditor _securityAuditor;
    private readonly Dictionary<string, Func<string?, Task<object>>> _tools;

    public CustomMcpServer(IAzureResourceManager resourceManager, ISecurityAuditor securityAuditor)
    {
        _resourceManager = resourceManager;
        _securityAuditor = securityAuditor;
        
        _tools = new Dictionary<string, Func<string?, Task<object>>>
        {
            ["GetResourceCosts"] = async (resourceGroupName) => await _resourceManager.GetResourceCostsAsync(resourceGroupName),
            ["GetResourceUtilization"] = async (resourceGroupName) => await _resourceManager.GetResourceUtilizationAsync(resourceGroupName),
            ["AuditResources"] = async (resourceGroupName) => await _securityAuditor.AuditResourcesAsync(resourceGroupName)
        };
    }

    public async Task HandleRequest(HttpContext context)
    {
        context.Response.Headers["Content-Type"] = "application/json";
        
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        
        try
        {
            var request = JsonDocument.Parse(body);
            var method = request.RootElement.GetProperty("method").GetString();
            var id = request.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetInt64() : 0;

            object response = null;

            switch (method)
            {
                case "initialize":
                    response = new
                    {
                        jsonrpc = "2.0",
                        id,
                        result = new
                        {
                            protocolVersion = "2024-11-05",
                            capabilities = new
                            {
                                tools = new { listChanged = true }
                            },
                            serverInfo = new
                            {
                                name = "AzureDevOpsAssistant",
                                version = "1.0.0.0"
                            }
                        }
                    };
                    break;

                case "tools/list":
                    var tools = _tools.Select(kvp => new
                    {
                        name = kvp.Key,
                        description = GetToolDescription(kvp.Key)
                    }).ToArray();

                    response = new
                    {
                        jsonrpc = "2.0",
                        id,
                        result = new { tools }
                    };
                    break;

                case "tools/call":
                    var paramsElement = request.RootElement.GetProperty("params");
                    var toolName = paramsElement.GetProperty("name").GetString();
                    var arguments = new Dictionary<string, object>();
                    
                    if (paramsElement.TryGetProperty("arguments", out var argsElement))
                    {
                        foreach (var prop in argsElement.EnumerateObject())
                        {
                            arguments[prop.Name] = prop.Value.ToString();
                        }
                    }

                    if (_tools.TryGetValue(toolName, out var toolFunc))
                    {
                        var resourceGroupName = arguments.ContainsKey("resourceGroupName") 
                            ? arguments["resourceGroupName"]?.ToString() 
                            : null;
                        
                        var result = await toolFunc(resourceGroupName);
                        
                        // Format the result for better readability
                        var formattedResult = FormatResult(toolName, result);
                        
                        response = new
                        {
                            jsonrpc = "2.0",
                            id,
                            result = formattedResult
                        };
                    }
                    else
                    {
                        response = new
                        {
                            jsonrpc = "2.0",
                            id,
                            error = new
                            {
                                code = -32601,
                                message = $"Tool not found: {toolName}"
                            }
                        };
                    }
                    break;

                default:
                    response = new
                    {
                        jsonrpc = "2.0",
                        id,
                        error = new
                        {
                            code = -32601,
                            message = $"Method not found: {method}"
                        }
                    };
                    break;
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            var errorResponse = new
            {
                jsonrpc = "2.0",
                id = 0,
                error = new
                {
                    code = -32603,
                    message = "Internal error",
                    data = ex.Message
                }
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        }
    }

    private string GetToolDescription(string toolName)
    {
        return toolName switch
        {
            "GetResourceCosts" => "Gets cost information for Azure resources with optional filtering by resource group",
            "GetResourceUtilization" => "Gets utilization information for Azure resources",
            "AuditResources" => "Performs security audit on Azure resources",
            _ => "Azure management tool"
        };
    }

    private object FormatResult(string toolName, object result)
    {
        if (toolName == "GetResourceCosts" && result != null)
        {
            // Cast to the expected structure and format
            var json = JsonSerializer.Serialize(result);
            var costInfo = JsonSerializer.Deserialize<JsonElement>(json);
            
            if (costInfo.TryGetProperty("TotalCost", out var totalCost) &&
                costInfo.TryGetProperty("Currency", out var currency) &&
                costInfo.TryGetProperty("ResourceCosts", out var resourceCosts))
            {
                var formattedCosts = new List<object>();
                foreach (var cost in resourceCosts.EnumerateArray())
                {
                    formattedCosts.Add(new
                    {
                        ResourceName = cost.GetProperty("ResourceName").GetString(),
                        ResourceType = cost.GetProperty("ResourceType").GetString(),
                        Cost = cost.GetProperty("Cost").GetDouble(),
                        Currency = cost.GetProperty("Currency").GetString()
                    });
                }

                return new
                {
                    Summary = $"Total Cost: {totalCost.GetDouble():C} {currency.GetString()}",
                    Period = $"{costInfo.GetProperty("PeriodStart").GetDateTime():yyyy-MM-dd} to {costInfo.GetProperty("PeriodEnd").GetDateTime():yyyy-MM-dd}",
                    Resources = formattedCosts
                };
            }
        }
        
        return result;
    }

    private string ExtractResourceLocation(string resourceId)
    {
        // For now, return a default location since Azure resource IDs don't consistently include location
        // In a real implementation, you would query the Azure Resource Graph or Resource Manager API
        // to get the actual location of each resource
        return "Azure Cloud";
    }
}
