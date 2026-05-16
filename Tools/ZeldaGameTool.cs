using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using StreamableHttpWebApp.Services;

namespace StreamableHttpWebApp.Tools;

[McpServerToolType]
public class ZeldaGameTool
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuditLogger _auditLogger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ZeldaGameTool> _logger;

    public ZeldaGameTool(IHttpClientFactory httpClientFactory, AuditLogger auditLogger, IHttpContextAccessor httpContextAccessor, ILogger<ZeldaGameTool> logger)
    {
        _httpClientFactory = httpClientFactory;
        _auditLogger = auditLogger;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

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

    [McpServerTool, Description("Get Zelda franchise games by name. Returns game details including developer, publisher, and release date.")]
    public async Task<List<ZeldaGame>> GetGames(
        [Description("Game name to search for (required)")] string name)
    {
        // RequireAuthentication(nameof(GetGames));

        var httpContext = _httpContextAccessor.HttpContext;
        var userId = UserContextHelper.GetUserId(httpContext);

        try
        {
            var parameters = new Dictionary<string, object?> { { "name", name } };
            _auditLogger.LogToolExecution(userId ?? "UNKNOWN", "GetGames", parameters);

            var client = _httpClientFactory.CreateClient("zeldaApi");
            using var response = await client.GetStreamAsync($"/api/games?name={Uri.EscapeDataString(name)}");
            using var jsonDoc = await JsonDocument.ParseAsync(response) ?? throw new McpException("no JSON returned from the games endpoint");

            var games = new List<ZeldaGame>();
            foreach (var element in jsonDoc.RootElement.GetProperty("data").EnumerateArray())
            {
                games.Add(new ZeldaGame
                {
                    Name = element.GetProperty("name").GetString(),
                    Description = element.GetProperty("description").GetString(),
                    Developer = element.GetProperty("developer").GetString(),
                    Publisher = element.GetProperty("publisher").GetString(),
                    ReleaseDate = element.GetProperty("released_date").GetString(),
                    Id = element.GetProperty("id").GetString()
                });
            }
            return games;
        }
        catch (Exception ex)
        {
            var parameters = new Dictionary<string, object?> { { "name", name } };
            _auditLogger.LogToolExecution(userId ?? "UNKNOWN", "GetGames", parameters, false, ex.Message);
            throw;
        }
    }
}
