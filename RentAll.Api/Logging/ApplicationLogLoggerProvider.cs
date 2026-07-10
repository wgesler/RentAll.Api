using Microsoft.Extensions.Options;

namespace RentAll.Api.Logging;

public class ApplicationLogLoggerProvider : ILoggerProvider
{
    private readonly IApplicationLogQueue _queue;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationLoggingSettings _settings;

    public ApplicationLogLoggerProvider(
        IApplicationLogQueue queue,
        IHttpContextAccessor httpContextAccessor,
        IOptions<ApplicationLoggingSettings> settings)
    {
        _queue = queue;
        _httpContextAccessor = httpContextAccessor;
        _settings = settings.Value;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new ApplicationLogLogger(categoryName, _queue, _httpContextAccessor, _settings);
    }

    public void Dispose()
    {
    }
}
