namespace StreamableHttpWebApp.Services;

/// <summary>
/// Helper class to extract user identity information from HTTP context claims.
/// </summary>
public static class UserContextHelper
{
    /// <summary>
    /// Extract the user's Auth0 subject (unique identifier) from JWT claims.
    /// </summary>
    public static string? GetUserId(HttpContext? httpContext)
    {
        if (httpContext?.User == null)
            return null;

        return httpContext.User.FindFirst("sub")?.Value ?? httpContext.User.Identity?.Name;
    }

    /// <summary>
    /// Extract the user's email from JWT claims (if available).
    /// </summary>
    public static string? GetUserEmail(HttpContext? httpContext)
    {
        if (httpContext?.User == null)
            return null;

        return httpContext.User.FindFirst("email")?.Value;
    }

    /// <summary>
    /// Extract the user's name from JWT claims (if available).
    /// </summary>
    public static string? GetUserName(HttpContext? httpContext)
    {
        if (httpContext?.User == null)
            return null;

        return httpContext.User.FindFirst("name")?.Value;
    }

    /// <summary>
    /// Check if user is authenticated.
    /// </summary>
    public static bool IsAuthenticated(HttpContext? httpContext)
    {
        return httpContext?.User.Identity?.IsAuthenticated ?? false;
    }

    /// <summary>
    /// Get a formatted user identifier for logging (includes email if available, otherwise uses sub).
    /// </summary>
    public static string GetFormattedUserId(HttpContext? httpContext)
    {
        var email = GetUserEmail(httpContext);
        var userId = GetUserId(httpContext);

        if (!string.IsNullOrEmpty(email))
        {
            return $"{email} ({userId})";
        }

        return userId ?? "UNKNOWN";
    }
}
