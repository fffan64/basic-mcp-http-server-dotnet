using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using StreamableHttpWebApp.Services;

namespace StreamableHttpWebApp.Tools;

[McpServerToolType]
public class ChuckNorrisJokeTool
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuditLogger _auditLogger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ChuckNorrisJokeTool> _logger;

    public ChuckNorrisJokeTool(IHttpClientFactory httpClientFactory, AuditLogger auditLogger, IHttpContextAccessor httpContextAccessor, ILogger<ChuckNorrisJokeTool> logger)
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

    [McpServerTool, Description("This tool returns a random joke and an icon URL from the https://api.chucknorris.io/ API.")]
    public async Task<ChuckNorrisJoke> GetChuckNorrisJoke()
    {
        RequireAuthentication(nameof(GetChuckNorrisJoke));

        var httpContext = _httpContextAccessor.HttpContext;
        var userId = UserContextHelper.GetUserId(httpContext);

        try
        {
            var client = _httpClientFactory.CreateClient("chuckNorrisApi");
            using var response = await client.GetStreamAsync("/jokes/random");
            using var jsonDoc = await JsonDocument.ParseAsync(response) ?? throw new McpException("no JSON returned from the Chuck Norris API");
            var joke = jsonDoc.RootElement.GetProperty("value").GetString() ?? string.Empty;
            var iconUrl = jsonDoc.RootElement.GetProperty("icon_url").GetString() ?? string.Empty;
            return new ChuckNorrisJoke { Value = joke, IconUrl = new Uri(iconUrl) };
        }
        catch (Exception ex)
        {
            _auditLogger.LogToolExecution(userId ?? "UNKNOWN", "GetChuckNorrisJoke", null, false, ex.Message);
            throw;
        }
    }
}
