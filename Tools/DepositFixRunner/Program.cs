using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Repositories.Accounting;
using RentAll.Infrastructure.Repositories.Common;
using RentAll.Infrastructure.Repositories.Contacts;
using RentAll.Infrastructure.Repositories.Health;
using RentAll.Infrastructure.Repositories.Maintenances;
using RentAll.Infrastructure.Repositories.Organizations;
using RentAll.Infrastructure.Repositories.Properties;
using RentAll.Infrastructure.Repositories.Reservations;

const string officeIds = "2"; // San Francisco
var orgId = Guid.Parse("280CD8DA-F1BE-41F2-AE6E-B45008CF3896");
var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
var appSettings = Options.Create(new AppSettings
{
    DbConnections =
    [
        new DbConnection
        {
            DbName = "RentAll",
            ConnectionString = "Server=GESLER;Database=RentAll;Trusted_Connection=True;Encrypt=False;MultipleActiveResultSets=True;"
        }
    ]
});

var health = new HealthRepository(appSettings);
var manager = new AccountingManager(
    new OrganizationRepository(appSettings),
    new PropertyRepository(appSettings),
    new AccountingRepository(appSettings, NullLogger<AccountingRepository>.Instance),
    new MaintenanceRepository(appSettings),
    new ReservationRepository(appSettings),
    new JournalEntryRepository(appSettings, NullLogger<JournalEntryRepository>.Instance),
    new OrganizationManager(new CommonRepository(appSettings), new OrganizationRepository(appSettings)),
    new ContactRepository(appSettings),
    new EnabledFeatureFlags(),
    health);

Console.WriteLine("=== SF (office 2) BEFORE ===");
await PrintHealthAsync(health, orgId, officeIds);

for (var pass = 1; pass <= 3; pass++)
{
    Console.WriteLine($"=== Pass {pass}: Fix Deposits ===");
    var depositFix = await manager.SyncJournalEntriesForHealthFixAsync(orgId, officeIds, "deposit", [], null, userId);
    Console.WriteLine($"Deposits Errors={depositFix.Errors.Count} Processed={depositFix.DocumentsProcessed}");

    Console.WriteLine($"=== Pass {pass}: Fix Transfers ===");
    var transferFix = await manager.SyncJournalEntriesForHealthFixAsync(orgId, officeIds, "transfer", [], null, userId);
    Console.WriteLine($"Transfers Errors={transferFix.Errors.Count} Processed={transferFix.DocumentsProcessed}");

    Console.WriteLine($"=== Pass {pass}: Fix Document Links ===");
    var linkFix = await manager.RepairDocumentLinksForHealthFixAsync(orgId, officeIds, userId);
    Console.WriteLine($"DocumentLinks Errors={linkFix.Errors.Count} Processed={linkFix.DocumentsProcessed}");
    if (linkFix.Errors.Count > 0)
    {
        foreach (var error in linkFix.Errors.Take(5))
            Console.WriteLine($"  {error}");
    }

    Console.WriteLine($"=== Pass {pass} health ===");
    var snapshot = await GetHealthSnapshotAsync(health, orgId, officeIds);
    await PrintHealthAsync(health, orgId, officeIds);
    if (snapshot.AllClean)
    {
        Console.WriteLine("=== CLEAN ===");
        break;
    }
}

static async Task<(bool AllClean, int DepositIssues, int TransferIssues, int LinkIssues)> GetHealthSnapshotAsync(
    HealthRepository health, Guid orgId, string officeIds)
{
    var deposits = await health.RunDepositHealthCheckAsync(orgId, officeIds);
    var transfers = await health.RunTransferHealthCheckAsync(orgId, officeIds);
    var links = await health.RunDocumentLinksHealthCheckAsync(orgId, officeIds);
    var depositIssues = deposits.Summary.DocumentsMissingJe;
    var transferIssues = transfers.Summary.DocumentsMissingJe;
    var linkIssues = links.Summary.DocumentsMissingJe;
    return (depositIssues == 0 && transferIssues == 0 && linkIssues == 0, depositIssues, transferIssues, linkIssues);
}

static async Task PrintHealthAsync(HealthRepository health, Guid orgId, string officeIds)
{
    await PrintRowAsync("Deposits", await health.RunDepositHealthCheckAsync(orgId, officeIds));
    await PrintRowAsync("Transfers", await health.RunTransferHealthCheckAsync(orgId, officeIds));
    await PrintRowAsync("Payments(Invoice)", await health.RunPaymentHealthCheckAsync(orgId, officeIds, (int)PaymentKind.Invoice));
    await PrintRowAsync("DocumentLinks", await health.RunDocumentLinksHealthCheckAsync(orgId, officeIds));
}

static Task PrintRowAsync(string label, DocumentHealthResult result)
{
    var s = result.Summary;
    Console.WriteLine($"{label}: Total={s.TotalDocuments} Issues={s.DocumentsMissingJe} Clean={s.IsClean}");
    return Task.CompletedTask;
}

file sealed class EnabledFeatureFlags : IFeatureFlagService
{
    public IReadOnlyDictionary<string, bool> GetAll() => new Dictionary<string, bool> { [FeatureFlagKeys.Accounting] = true };
    public bool IsEnabled(string f) => true;
    public Task<bool> IsEnabledAsync(string f, Guid o, CancellationToken c = default) => Task.FromResult(true);
    public void Set(string f, bool e) { }
}
