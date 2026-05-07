using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using StreamableHttpWebApp.Services;

namespace StreamableHttpWebApp.Tools;

[McpServerToolType]
public class DisneyCharacterTool
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuditLogger _auditLogger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DisneyCharacterTool> _logger;

    public DisneyCharacterTool(IHttpClientFactory httpClientFactory, AuditLogger auditLogger, IHttpContextAccessor httpContextAccessor, ILogger<DisneyCharacterTool> logger)
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

    [McpServerTool, Description("Get all Disney characters or filter by name. Returns character name, list of films, and image URL.")]
    public async Task<List<DisneyCharacter>> GetCharacters(
        [Description("Optional character name to filter results")] string? name = null)
    {
        RequireAuthentication(nameof(GetCharacters));

        var httpContext = _httpContextAccessor.HttpContext;
        var userId = UserContextHelper.GetUserId(httpContext);

        try
        {
            var parameters = new Dictionary<string, object?> { { "name", name } };
            _auditLogger.LogToolExecution(userId ?? "UNKNOWN", "GetCharacters", parameters);

            var client = _httpClientFactory.CreateClient("disneyApi");
            var url = name is not null ? $"/character?name={Uri.EscapeDataString(name)}" : "/character";

            using var response = await client.GetStreamAsync(url);
            using var jsonDoc = await JsonDocument.ParseAsync(response) ?? throw new McpException("no JSON returned from the character endpoint");

            var characters = new List<DisneyCharacter>();
            foreach (var element in jsonDoc.RootElement.GetProperty("data").EnumerateArray())
            {
                characters.Add(new DisneyCharacter
                {
                    Id = element.GetProperty("_id").GetInt32(),
                    Name = element.GetProperty("name").GetString(),
                    Films = element.TryGetProperty("films", out var filmsElement) && filmsElement.ValueKind == JsonValueKind.Array
                        ? filmsElement.EnumerateArray().Select(f => f.GetString() ?? string.Empty).ToList()
                        : new List<string>(),
                    ImageUrl = element.TryGetProperty("imageUrl", out var imageUrlElement) && imageUrlElement.ValueKind == JsonValueKind.String
                        ? imageUrlElement.GetString()
                        : null
                });
            }
            return characters;
        }
        catch (Exception ex)
        {
            var parameters = new Dictionary<string, object?> { { "name", name } };
            _auditLogger.LogToolExecution(userId ?? "UNKNOWN", "GetCharacters", parameters, false, ex.Message);
            throw;
        }
    }
}
