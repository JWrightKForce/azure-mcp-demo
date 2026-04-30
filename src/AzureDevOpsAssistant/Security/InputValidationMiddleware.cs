using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AzureDevOpsAssistant.Security;

public class InputValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputValidationMiddleware> _logger;
    private readonly InputValidationOptions _options;

    public InputValidationMiddleware(
        RequestDelegate next,
        ILogger<InputValidationMiddleware> logger,
        IOptions<InputValidationOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate POST/PUT requests
        if (context.Request.Method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase) ||
            context.Request.Method.Equals(HttpMethods.Put, StringComparison.OrdinalIgnoreCase))
        {
            context.Request.EnableBuffering();
            
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (!IsValidInput(body))
            {
                _logger.LogWarning("Invalid input detected: {Body}", body.Substring(0, Math.Min(100, body.Length)));
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid input detected");
                return;
            }
        }

        await _next(context);
    }

    private bool IsValidInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return true;

        // Check for SQL injection patterns
        var sqlPatterns = new[]
        {
            "DROP TABLE", "DELETE FROM", "INSERT INTO", "UPDATE SET", "UNION SELECT",
            "EXEC(", "EXECUTE", "SP_EXECUTESQL", "--", "/*", "*/", "XP_"
        };

        foreach (var pattern in sqlPatterns)
        {
            if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // Check for XSS patterns
        var xssPatterns = new[]
        {
            "<script>", "</script>", "javascript:", "onload=", "onerror=", "onclick=",
            "onmouseover=", "onfocus=", "onblur=", "onchange=", "onsubmit="
        };

        foreach (var pattern in xssPatterns)
        {
            if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        // Check for command injection
        var cmdPatterns = new[]
        {
            "&&", "||", ";", "|", "`", "$(", "${", ">", ">>", "<"
        };

        foreach (var pattern in cmdPatterns)
        {
            if (input.Contains(pattern))
                return false;
        }

        // Check input length
        if (input.Length > _options.MaxInputLength)
            return false;

        // Try to parse as JSON to ensure it's valid
        if (input.StartsWith("{") || input.StartsWith("["))
        {
            try
            {
                JsonDocument.Parse(input);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        return true;
    }
}

public class InputValidationOptions
{
    public int MaxInputLength { get; set; } = 10000;
}
