using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using StreamableHttpWebApp.Services;

namespace StreamableHttpWebApp.Tools;

[McpServerToolType]
public class WeatherAlertsTool
{
  private readonly IHttpClientFactory _httpClientFactory;
  private readonly AuditLogger _auditLogger;
  private readonly IHttpContextAccessor _httpContextAccessor;
  private readonly ILogger<WeatherAlertsTool> _logger;

  public WeatherAlertsTool(IHttpClientFactory httpClientFactory, AuditLogger auditLogger, IHttpContextAccessor httpContextAccessor, ILogger<WeatherAlertsTool> logger)
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

  [McpServerTool, Description("Hello world from Streamable http tool!")]
  public string GetHelloWorld()
  {
    RequireAuthentication(nameof(GetHelloWorld));

    var httpContext = _httpContextAccessor.HttpContext;
    var userId = UserContextHelper.GetUserId(httpContext);

    try
    {
      _auditLogger.LogToolExecution(userId ?? "UNKNOWN", "GetHelloWorld");
      return "Hello world from Streamable http tool!";
    }
    catch (Exception ex)
    {
      _auditLogger.LogToolExecution(userId ?? "UNKNOWN", "GetHelloWorld", null, false, ex.Message);
      throw;
    }
  }

  [McpServerTool, Description("This tool returns alerts from the https://api.weather.gov/ API based on the state code.")]
  public async Task<List<WeatherAlert>> GetAlerts([Description("2 character state code, e.g. CA for California")] string stateCode)
  {
    RequireAuthentication(nameof(GetAlerts));

    var httpContext = _httpContextAccessor.HttpContext;
    var userId = UserContextHelper.GetUserId(httpContext);

    try
    {
      var parameters = new Dictionary<string, object?> { { "stateCode", stateCode } };
      _auditLogger.LogToolExecution(userId ?? "UNKNOWN", "GetAlerts", parameters);

      var client = _httpClientFactory.CreateClient("weatherApi");
      using var response = await client.GetStreamAsync($"/alerts?area={stateCode}&limit=10");
      using var jsonDoc = await JsonDocument.ParseAsync(response) ?? throw new McpException("no JSON returned from the alerts endpoint");
      var alerts = new List<WeatherAlert>();
      foreach (var element in jsonDoc.RootElement.GetProperty("features").EnumerateArray())
      {
        alerts.Add(new WeatherAlert
        {
          Event = element.GetProperty("properties").GetProperty("event").GetString() ?? string.Empty,
          AreaDesc = element.GetProperty("properties").GetProperty("areaDesc").GetString() ?? string.Empty,
          Severity = element.GetProperty("properties").GetProperty("severity").GetString() ?? string.Empty,
          Description = element.GetProperty("properties").GetProperty("description").GetString() ?? string.Empty
        });
      }
      return alerts;
    }
    catch (Exception ex)
    {
      var parameters = new Dictionary<string, object?> { { "stateCode", stateCode } };
      _auditLogger.LogToolExecution(userId ?? "UNKNOWN", "GetAlerts", parameters, false, ex.Message);
      throw;
    }
  }
}