using System.Text.Json;

namespace StreamableHttpWebApp.Services;

/// <summary>
/// Audit logging service for tracking authenticated tool executions.
/// Logs user identity, tool name, parameters, and timestamp to a JSON file.
/// </summary>
public class AuditLogger
{
    private readonly string _logsDirectory;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(ILogger<AuditLogger> logger)
    {
        _logger = logger;
        _logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

        // Create logs directory if it doesn't exist
        if (!Directory.Exists(_logsDirectory))
        {
            Directory.CreateDirectory(_logsDirectory);
        }
    }

    /// <summary>
    /// Log a tool execution with user context and parameters.
    /// </summary>
    public void LogToolExecution(string userId, string toolName, Dictionary<string, object?>? parameters = null, bool success = true, string? errorMessage = null)
    {
        try
        {
            var logEntry = new
            {
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                ToolName = toolName,
                Parameters = parameters ?? new Dictionary<string, object?>(),
                Success = success,
                ErrorMessage = errorMessage
            };

            var logPath = Path.Combine(_logsDirectory, $"audit-{DateTime.UtcNow:yyyy-MM-dd}.log");
            var jsonLine = JsonSerializer.Serialize(logEntry);

            // Append to log file (thread-safe)
            lock (this)
            {
                File.AppendAllText(logPath, jsonLine + Environment.NewLine);
            }

            _logger.LogInformation("Tool execution logged: {ToolName} by {UserId}", toolName, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log");
        }
    }

    /// <summary>
    /// Log unauthorized access attempt.
    /// </summary>
    public void LogUnauthorizedAccess(string? identityInfo, string toolName, string? reason = null)
    {
        try
        {
            var logEntry = new
            {
                Timestamp = DateTime.UtcNow,
                IdentityInfo = identityInfo ?? "UNKNOWN",
                ToolName = toolName,
                Status = "UNAUTHORIZED",
                Reason = reason
            };

            var logPath = Path.Combine(_logsDirectory, $"audit-{DateTime.UtcNow:yyyy-MM-dd}.log");
            var jsonLine = JsonSerializer.Serialize(logEntry);

            lock (this)
            {
                File.AppendAllText(logPath, jsonLine + Environment.NewLine);
            }

            _logger.LogWarning("Unauthorized access attempt to {ToolName}: {Reason}", toolName, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write unauthorized access log");
        }
    }
}
