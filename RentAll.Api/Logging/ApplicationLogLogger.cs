using System.Text;
using System.Text.Json;

namespace RentAll.Api.Logging;

public class ApplicationLogLogger : ILogger
{
    private const int MaxMessageLength = 2500;
    private const string LoggingNamespace = "RentAll.Api.Logging";
    private readonly string _categoryName;
    private readonly IApplicationLogQueue _queue;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationLoggingSettings _settings;

    public ApplicationLogLogger(
        string categoryName,
        IApplicationLogQueue queue,
        IHttpContextAccessor httpContextAccessor,
        ApplicationLoggingSettings settings)
    {
        _categoryName = categoryName;
        _queue = queue;
        _httpContextAccessor = httpContextAccessor;
        _settings = settings;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        if (!_settings.Enabled)
            return false;

        if (_categoryName.StartsWith(LoggingNamespace, StringComparison.Ordinal))
            return false;

        if (_categoryName.StartsWith("RentAll.Infrastructure.Repositories.Logging", StringComparison.Ordinal))
            return false;

        return logLevel >= _settings.MinimumLevel;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var message = formatter(state, exception);
            if (string.IsNullOrWhiteSpace(message) && exception != null)
                message = exception.Message;

            if (string.IsNullOrWhiteSpace(message))
                return;

            var log = new ApplicationLog
            {
                Level = logLevel.ToString(),
                Category = _categoryName,
                EventId = eventId.Id == 0 ? null : eventId.Id,
                OrganizationId = ResolveOrganizationId(httpContext),
                OfficeId = ResolveInt(httpContext, "officeId"),
                TraceId = httpContext?.TraceIdentifier,
                Message = Truncate(message),
                Exception = exception?.ToString(),
                Properties = BuildPropertiesJson(state)
            };

            _queue.TryEnqueue(log);
        }
        catch
        {
            // Never throw from ILogger pipeline.
        }
    }

    private static string? BuildPropertiesJson<TState>(TState state)
    {
        if (state is not IEnumerable<KeyValuePair<string, object?>> structuredState)
            return null;

        var properties = structuredState
            .Where(kv => !string.Equals(kv.Key, "{OriginalFormat}", StringComparison.Ordinal))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        if (properties.Count == 0)
            return null;

        return JsonSerializer.Serialize(properties);
    }

    private static Guid? ResolveOrganizationId(HttpContext? context)
    {
        var organizationId = ResolveGuid(context, "organizationId");
        if (organizationId.HasValue)
            return organizationId.Value;

        var userClaim = context?.User.FindFirst("user");
        if (userClaim == null || string.IsNullOrWhiteSpace(userClaim.Value))
            return null;

        try
        {
            var userJsonBytes = Convert.FromBase64String(userClaim.Value);
            var userJson = Encoding.UTF8.GetString(userJsonBytes);
            var userObject = JsonSerializer.Deserialize<JsonElement>(userJson);

            string[] possibleOrgPropertyNames = ["organizationId", "OrganizationId"];
            foreach (var propName in possibleOrgPropertyNames)
            {
                if (!userObject.TryGetProperty(propName, out var orgIdElement))
                    continue;

                var orgIdString = orgIdElement.GetString();
                if (!string.IsNullOrWhiteSpace(orgIdString) && Guid.TryParse(orgIdString, out var orgId))
                    return orgId;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static Guid? ResolveGuid(HttpContext? context, string key)
    {
        if (TryGetValue(context, key, out var rawValue) && Guid.TryParse(rawValue, out var value))
            return value;

        return null;
    }

    private static int? ResolveInt(HttpContext? context, string key)
    {
        if (TryGetValue(context, key, out var rawValue) && int.TryParse(rawValue, out var value))
            return value;

        return null;
    }

    private static bool TryGetValue(HttpContext? context, string key, out string value)
    {
        if (context == null)
        {
            value = string.Empty;
            return false;
        }

        var routeMatch = context.Request.RouteValues
            .FirstOrDefault(routeValue => routeValue.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(routeMatch.Key) && routeMatch.Value != null)
        {
            value = routeMatch.Value.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(value))
                return true;
        }

        var queryMatch = context.Request.Query
            .FirstOrDefault(queryValue => queryValue.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(queryMatch.Key) && !string.IsNullOrWhiteSpace(queryMatch.Value))
        {
            value = queryMatch.Value.ToString();
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string Truncate(string value)
    {
        return value.Length <= MaxMessageLength ? value : value[..MaxMessageLength];
    }
}
