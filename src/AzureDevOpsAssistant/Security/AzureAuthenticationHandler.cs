using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace AzureDevOpsAssistant.Security;

public class AzureAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ILogger<AzureAuthenticationHandler> _logger;

    public AzureAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, loggerFactory, encoder, clock)
    {
        _logger = loggerFactory.CreateLogger<AzureAuthenticationHandler>();
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            // For demo purposes, we'll accept the authentication token from Azure AD
            // In production, this would validate the JWT token properly
            var authorizationHeader = Request.Headers["Authorization"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing or invalid authorization header"));
            }

            var token = authorizationHeader.Substring("Bearer ".Length).Trim();
            
            // Validate token (simplified for demo)
            if (string.IsNullOrEmpty(token))
            {
                return Task.FromResult(AuthenticateResult.Fail("Empty token"));
            }

            // Create claims based on the token (simplified for demo)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "demo-user"),
                new Claim(ClaimTypes.Name, "Demo User"),
                new Claim(ClaimTypes.Role, "AzureDevOpsAssistant"),
                new Claim("azp", "mcp-demo-client"),
                new Claim("appid", "demo-app-id")
            };

            var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, JwtBearerDefaults.AuthenticationScheme);

            _logger.LogInformation("User authenticated successfully");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed");
            return Task.FromResult(AuthenticateResult.Fail("Authentication failed"));
        }
    }
}
