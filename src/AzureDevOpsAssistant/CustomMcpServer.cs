using System.Text.Json;
using AzureDevOpsAssistant.Services.Interfaces;

namespace AzureDevOpsAssistant;

public class CustomMcpServer
{
    private readonly IAzureResourceManager _resourceManager;
    private readonly ISecurityAuditor _securityAuditor;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Func<Dictionary<string, object>, Task<object>>> _tools;

    public CustomMcpServer(IAzureResourceManager resourceManager, ISecurityAuditor securityAuditor, IServiceProvider serviceProvider)
    {
        _resourceManager = resourceManager;
        _securityAuditor = securityAuditor;
        _serviceProvider = serviceProvider;
        
        _tools = new Dictionary<string, Func<Dictionary<string, object>, Task<object>>>
        {
            ["GetResourceCosts"] = async (arguments) => await _resourceManager.GetResourceCostsAsync(arguments.ContainsKey("resourceGroupName") ? arguments["resourceGroupName"]?.ToString() : null),
            ["GetResourceUtilization"] = async (arguments) => await AzureDevOpsAssistant.Tools.ResourceAnalyzerTool.GetResourceUtilization(
                arguments.ContainsKey("resourceGroupName") ? arguments["resourceGroupName"]?.ToString() : null,
                arguments.ContainsKey("resourceTypeFilter") ? arguments["resourceTypeFilter"]?.ToString() : null,
                _serviceProvider),
            ["GetDatabases"] = async (arguments) => await AzureDevOpsAssistant.Tools.ResourceAnalyzerTool.GetDatabases(arguments.ContainsKey("resourceGroupName") ? arguments["resourceGroupName"]?.ToString() : null, _serviceProvider),
            ["AuditResources"] = async (arguments) => await _securityAuditor.AuditResourcesAsync(arguments.ContainsKey("resourceGroupName") ? arguments["resourceGroupName"]?.ToString() : null)
        };
    }

    public async Task HandleRequest(HttpContext context)
    {
        context.Response.Headers["Content-Type"] = "application/json";
        
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        
        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now}] 🔧 MCP Server: Received request: {body}");
        
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
                        var result = await toolFunc(arguments);
                        
                        response = new
                        {
                            jsonrpc = "2.0",
                            id,
                            result
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
            "GetDatabases" => "Gets database resources from Azure",
            "AuditResources" => "Performs security audit on Azure resources",
            _ => "Azure management tool"
        };
    }
}
