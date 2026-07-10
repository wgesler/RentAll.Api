namespace RentAll.Api.Logging;

public interface IApplicationLogQueue
{
    bool TryEnqueue(ApplicationLog log);
    IAsyncEnumerable<ApplicationLog> ReadAllAsync(CancellationToken cancellationToken);
}
