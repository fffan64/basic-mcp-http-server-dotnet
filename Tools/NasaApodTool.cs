using System;
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using StreamableHttpWebApp.Services;

namespace StreamableHttpWebApp.Tools;

[McpServerToolType]
public class NasaApodTool
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuditLogger _auditLogger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<NasaApodTool> _logger;

    public NasaApodTool(IHttpClientFactory httpClientFactory, AuditLogger auditLogger, IHttpContextAccessor httpContextAccessor, ILogger<NasaApodTool> logger, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _auditLogger = auditLogger;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _apiKey = configuration["NasaApod:ApiKey"] ?? throw new ArgumentNullException("NasaApod:ApiKey");
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

    private readonly string _apiKey;

    [McpServerTool, Description("Get the Astronomy Picture of the Day from NASA. Returns today's APOD.")]
    public async Task<NasaApod> GetApod()
    {
        RequireAuthentication(nameof(GetApod));

        var httpContext = _httpContextAccessor.HttpContext;
        var userId = UserContextHelper.GetUserId(httpContext);

        try
        {
            _auditLogger.LogToolExecution(userId ?? "UNKNOWN", "GetApod");

            var client = _httpClientFactory.CreateClient("nasaApodApi");
            using var response = await client.GetStreamAsync($"/planetary/apod?api_key={_apiKey}");
            using var jsonDoc = await JsonDocument.ParseAsync(response) ?? throw new McpException("no JSON returned from the NASA APOD API");

            return new NasaApod
            {
                Title = jsonDoc.RootElement.GetProperty("title").GetString(),
                Date = jsonDoc.RootElement.GetProperty("date").GetString(),
                Url = jsonDoc.RootElement.GetProperty("url").GetString(),
                HdUrl = jsonDoc.RootElement.TryGetProperty("hdurl", out var hdurl) ? hdurl.GetString() : null,
                MediaType = jsonDoc.RootElement.GetProperty("media_type").GetString(),
                Explanation = jsonDoc.RootElement.GetProperty("explanation").GetString(),
                Copyright = jsonDoc.RootElement.TryGetProperty("copyright", out var copyright) ? copyright.GetString() : null,
                ThumbnailUrl = jsonDoc.RootElement.TryGetProperty("thumbnail_url", out var thumbnailUrl) ? thumbnailUrl.GetString() : null
            };
        }
        catch (Exception ex)
        {
            _auditLogger.LogToolExecution(userId ?? "UNKNOWN", "GetApod", null, false, ex.Message);
            throw;
        }
    }

    [McpServerTool, Description("Get the Astronomy Picture of the Day from NASA for a specific date.")]
    public async Task<NasaApod> GetApodByDate(
        [Description("Specific date in YYYY-MM-DD format")] string date)
    {
        RequireAuthentication(nameof(GetApodByDate));

        var httpContext = _httpContextAccessor.HttpContext;
        var userId = UserContextHelper.GetUserId(httpContext);

        try
        {
            var parameters = new Dictionary<string, object?> { { "date", date } };
            _auditLogger.LogToolExecution(userId ?? "UNKNOWN", "GetApodByDate", parameters);

            var client = _httpClientFactory.CreateClient("nasaApodApi");
            using var response = await client.GetStreamAsync($"/planetary/apod?api_key={_apiKey}&date={Uri.EscapeDataString(date)}");
            using var jsonDoc = await JsonDocument.ParseAsync(response) ?? throw new McpException("no JSON returned from the NASA APOD API");

            return new NasaApod
            {
                Title = jsonDoc.RootElement.GetProperty("title").GetString(),
                Date = jsonDoc.RootElement.GetProperty("date").GetString(),
                Url = jsonDoc.RootElement.GetProperty("url").GetString(),
                HdUrl = jsonDoc.RootElement.TryGetProperty("hdurl", out var hdurl) ? hdurl.GetString() : null,
                MediaType = jsonDoc.RootElement.GetProperty("media_type").GetString(),
                Explanation = jsonDoc.RootElement.GetProperty("explanation").GetString(),
                Copyright = jsonDoc.RootElement.TryGetProperty("copyright", out var copyright) ? copyright.GetString() : null,
                ThumbnailUrl = jsonDoc.RootElement.TryGetProperty("thumbnail_url", out var thumbnailUrl) ? thumbnailUrl.GetString() : null
            };
        }
        catch (Exception ex)
        {
            var parameters = new Dictionary<string, object?> { { "date", date } };
            _auditLogger.LogToolExecution(userId ?? "UNKNOWN", "GetApodByDate", parameters, false, ex.Message);
            throw;
        }
    }

    [McpServerTool, Description("Get multiple Astronomy Pictures of the Day from NASA.")]
    public async Task<List<NasaApod>> GetMultipleApods(
        [Description("Number of APODs to return (1-100)")] int count)
    {
        RequireAuthentication(nameof(GetMultipleApods));

        var httpContext = _httpContextAccessor.HttpContext;
        var userId = UserContextHelper.GetUserId(httpContext);

        try
        {
            if (count < 1 || count > 100)
            {
                throw new McpException("Count must be between 1 and 100");
            }

            var parameters = new Dictionary<string, object?> { { "count", count } };
            _auditLogger.LogToolExecution(userId ?? "UNKNOWN", "GetMultipleApods", parameters);

            var client = _httpClientFactory.CreateClient("nasaApodApi");
            using var response = await client.GetStreamAsync($"/planetary/apod?api_key={_apiKey}&count={count}");
            using var jsonDoc = await JsonDocument.ParseAsync(response) ?? throw new McpException("no JSON returned from the NASA APOD API");

            var apods = new List<NasaApod>();
            foreach (var element in jsonDoc.RootElement.EnumerateArray())
            {
                apods.Add(new NasaApod
                {
                    Title = element.GetProperty("title").GetString(),
                    Date = element.GetProperty("date").GetString(),
                    Url = element.GetProperty("url").GetString(),
                    HdUrl = element.TryGetProperty("hdurl", out var hdurl) ? hdurl.GetString() : null,
                    MediaType = element.GetProperty("media_type").GetString(),
                    Explanation = element.GetProperty("explanation").GetString(),
                    Copyright = element.TryGetProperty("copyright", out var copyright) ? copyright.GetString() : null,
                    ThumbnailUrl = element.TryGetProperty("thumbnail_url", out var thumbnailUrl) ? thumbnailUrl.GetString() : null
                });
            }
            return apods;
        }
        catch (Exception ex)
        {
            var parameters = new Dictionary<string, object?> { { "count", count } };
            _auditLogger.LogToolExecution(userId ?? "UNKNOWN", "GetMultipleApods", parameters, false, ex.Message);
            throw;
        }
    }
}