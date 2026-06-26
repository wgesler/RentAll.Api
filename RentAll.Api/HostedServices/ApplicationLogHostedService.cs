using RentAll.Api.Logging;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.HostedServices;

public class ApplicationLogHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IApplicationLogQueue _queue;

    public ApplicationLogHostedService(IServiceScopeFactory scopeFactory, IApplicationLogQueue queue)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var log in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var loggingRepository = scope.ServiceProvider.GetRequiredService<ILoggingRepository>();
                await loggingRepository.AddApplicationLogAsync(log);
            }
            catch
            {
                // Ignore to avoid logging loops.
            }
        }
    }
}
