using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using StreamableHttpWebApp.Services;

namespace StreamableHttpWebApp.Tools;

[McpServerToolType]
public class CatAsAServiceTool
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuditLogger _auditLogger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CatAsAServiceTool> _logger;

    public CatAsAServiceTool(IHttpClientFactory httpClientFactory, AuditLogger auditLogger, IHttpContextAccessor httpContextAccessor, ILogger<CatAsAServiceTool> logger)
    {
        _httpClientFactory = httpClientFactory;
        _auditLogger = auditLogger;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Check if the current request is authenticated. If not, log unauthorized attempt and throw.
    /// </summary>
    private void RequireAuthentication(string toolName)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var isAuthenticated = UserContextHelper.IsAuthenticated(httpContext);
        var authHeader = httpContext?.Request.Headers["Authorization"].ToString() ?? "NO_TOKEN";

        _logger.LogInformation("Authentication check for {ToolName}: IsAuthenticated={IsAuthenticated}, AuthHeader present={HasAuth}",
          toolName, isAuthenticated, !string.IsNullOrEmpty(authHeader) && authHeader != "NO_TOKEN");

        if (!isAuthenticated)
        {
            _logger.LogWarning("Unauthorized access attempt to {ToolName}. Auth header: {AuthHeader}", toolName, authHeader);
            _auditLogger.LogUnauthorizedAccess(authHeader, toolName, "Missing or invalid authentication token");
            throw new McpException("Unauthorized: Authentication token required");
        }
    }

    [McpServerTool, Description("This tool returns a random cat image URL from the https://cataas.com/ API.")]
    public async Task<CatAAS> GetRandomCatImage()
    {
        RequireAuthentication(nameof(GetRandomCatImage));

        var httpContext = _httpContextAccessor.HttpContext;
        var userId = UserContextHelper.GetUserId(httpContext);

        try
        {
            var client = _httpClientFactory.CreateClient("CatAsAServiceApi");
            using var response = await client.GetStreamAsync("/cat?json=true");
            using var jsonDoc = await JsonDocument.ParseAsync(response) ?? throw new McpException("no JSON returned from the Cat as a Service API");
            var imageUrl = jsonDoc.RootElement.GetProperty("url").GetString() ?? string.Empty;
            return new CatAAS { ImageUrl = new Uri(imageUrl) };
        }
        catch (Exception ex)
        {
            _auditLogger.LogToolExecution(userId ?? "UNKNOWN", "GetRandomCatImage", null, false, ex.Message);
            throw;
        }
    }
}
