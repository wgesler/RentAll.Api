using RentAll.Api.Services;

namespace RentAll.Api.HostedServices;

public class ExternalPropertyPhotoImportHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExternalPropertyPhotoImportHostedService> _logger;

    public ExternalPropertyPhotoImportHostedService(IServiceScopeFactory scopeFactory, ILogger<ExternalPropertyPhotoImportHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ExternalPropertyPhotoImportProcessor>();
                var processed = await processor.ProcessNextItemAsync(stoppingToken);

                if (!processed)
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External property photo import worker cycle failed");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
