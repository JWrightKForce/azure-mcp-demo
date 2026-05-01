extern alias AzureIdentity;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Azure.Core;
using AzureDevOpsAssistant.Services;
using AzureDevOpsAssistant.Services.Interfaces;

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

// Register application services
builder.Services.AddSingleton<IAzureResourceManager, AzureResourceManager>();
builder.Services.AddSingleton<ISecurityAuditor, SecurityAuditor>();
builder.Services.AddSingleton<IDeploymentOrchestrator, DeploymentOrchestrator>();
builder.Services.AddSingleton<ILogAnalyzer, LogAnalyzer>();
builder.Services.AddSingleton<IBackupManager, BackupManager>();

// Configure MCP Server with HTTP transport (v1.2.0)
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

// Add CORS middleware
app.UseCors();

// Health check endpoint
app.MapGet("/health", () => Results.Ok("healthy"));

// Test endpoint
app.MapGet("/test", () => Results.Json(new { message = "MCP Server is running", timestamp = DateTime.UtcNow }));

// Try to use MapMcp() for future compatibility, but always add fallback due to known bug
try
{
    app.MapMcp();
    app.Logger.LogInformation("MapMcp() succeeded - MCP endpoints auto-created");
}
catch (Exception ex)
{
    app.Logger.LogWarning($"MapMcp() failed: {ex.Message}. Using fallback endpoint.");
}

// Always add fallback endpoint - MapMcp() has known bug where it reports success but doesn't create endpoints
app.MapPost("/mcp", async (HttpContext context) =>
    {
        context.Response.Headers["Content-Type"] = "application/json";
        
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        
        try
        {
            var request = System.Text.Json.JsonDocument.Parse(body);
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
                                name = "Azure DevOps Assistant",
                                version = "1.0.0"
                            }
                        }
                    };
                    break;

                case "tools/list":
                    response = new
                    {
                        jsonrpc = "2.0",
                        id,
                        result = new
                        {
                            tools = new object[]
                            {
                                new
                                {
                                    name = "GetResourceCosts",
                                    description = "Get cost information for Azure resources with optional filtering by resource group",
                                    inputSchema = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            resourceGroupName = new
                                            {
                                                type = "string",
                                                description = "Optional resource group name to filter costs by"
                                            }
                                        }
                                    }
                                },
                                new
                                {
                                    name = "GetResourceUtilization",
                                    description = "Get utilization information for Azure resources",
                                    inputSchema = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            resourceGroupName = new
                                            {
                                                type = "string",
                                                description = "Optional resource group name to filter resources by"
                                            },
                                            resourceTypeFilter = new
                                            {
                                                type = "string",
                                                description = "Optional resource type filter (e.g., 'Microsoft.Web/sites')"
                                            }
                                        }
                                    }
                                },
                                new
                                {
                                    name = "AuditResources",
                                    description = "Perform security audit on Azure resources",
                                    inputSchema = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            resourceGroupName = new
                                            {
                                                type = "string",
                                                description = "Optional resource group name to filter audit by"
                                            }
                                        }
                                    }
                                }
                            }
                        }
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

                    var serviceProvider = context.RequestServices;
                    var resourceManager = serviceProvider.GetRequiredService<IAzureResourceManager>();
                    var securityAuditor = serviceProvider.GetRequiredService<ISecurityAuditor>();

                    switch (toolName)
                    {
                        case "GetResourceCosts":
                            var resourceGroupName = arguments.GetValueOrDefault("resourceGroupName")?.ToString();
                            var costResult = await resourceManager.GetResourceCostsAsync(resourceGroupName);
                            
                            var costToolOutput = new 
                            {
                                summary = $"Total Cost: {costResult.TotalCost:C} USD for {costResult.ResourceCosts.Count} resources from {costResult.PeriodStart:yyyy-MM-dd} to {costResult.PeriodEnd:yyyy-MM-dd}",
                                totalCost = costResult.TotalCost,
                                currency = costResult.Currency,
                                periodStart = costResult.PeriodStart,
                                periodEnd = costResult.PeriodEnd,
                                resources = costResult.ResourceCosts.Select(rc => new 
                                {
                                    rc.ResourceName,
                                    rc.ResourceType,
                                    rc.Cost,
                                    rc.Currency
                                })
                            };
                            
                            response = new
                            {
                                jsonrpc = "2.0",
                                id,
                                result = new
                                {
                                    content = new[]
                                    {
                                        new
                                        {
                                            type = "text",
                                            text = System.Text.Json.JsonSerializer.Serialize(costToolOutput, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })
                                        }
                                    }
                                }
                            };
                            break;

                        case "GetResourceUtilization":
                            var utilResourceGroupName = arguments.GetValueOrDefault("resourceGroupName")?.ToString();
                            var resourceTypeFilter = arguments.GetValueOrDefault("resourceTypeFilter")?.ToString();
                            
                            var utilResult = await resourceManager.GetResourceUtilizationAsync(utilResourceGroupName);
                            
                            if (!string.IsNullOrEmpty(resourceTypeFilter))
                            {
                                utilResult = utilResult.Where(r => r.Type.Contains(resourceTypeFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                            }
                            
                            var utilToolOutput = new 
                            {
                                totalResources = utilResult.Count,
                                resources = utilResult
                            };
                            
                            response = new
                            {
                                jsonrpc = "2.0",
                                id,
                                result = new
                                {
                                    content = new[]
                                    {
                                        new
                                        {
                                            type = "text",
                                            text = System.Text.Json.JsonSerializer.Serialize(utilToolOutput, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })
                                        }
                                    }
                                }
                            };
                            break;

                        case "AuditResources":
                            var auditResourceGroupName = arguments.GetValueOrDefault("resourceGroupName")?.ToString();
                            var auditResult = await securityAuditor.AuditResourcesAsync(auditResourceGroupName);
                            
                            var auditToolOutput = new 
                            {
                                auditResult.SecurityScore,
                                assessment = (auditResult.SecurityScore >= 80 ? "Good" : auditResult.SecurityScore >= 60 ? "Fair" : "Poor"),
                                auditResult.TotalResources,
                                auditResult.ResourcesWithIssues,
                                issues = auditResult.Issues
                            };
                            
                            response = new
                            {
                                jsonrpc = "2.0",
                                id,
                                result = new
                                {
                                    content = new[]
                                    {
                                        new
                                        {
                                            type = "text",
                                            text = System.Text.Json.JsonSerializer.Serialize(auditToolOutput, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })
                                        }
                                    }
                                }
                            };
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
                    break;
            }

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
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

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
        }
    });

app.Run();
