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

static bool ArgsUseNoCoOffice(string[] args) =>
    args.Any(a => string.Equals(a, "noco", StringComparison.OrdinalIgnoreCase)
        || string.Equals(a, "noco-fix", StringComparison.OrdinalIgnoreCase)
        || a.StartsWith("noco-", StringComparison.OrdinalIgnoreCase));

var officeIds = args.FirstOrDefault(a => a.StartsWith("office=", StringComparison.OrdinalIgnoreCase)) is { } officeArg
    ? officeArg["office=".Length..]
    : ArgsUseNoCoOffice(args) ? "5"
    : "2"; // default San Francisco
var orgId = Guid.Parse("280CD8DA-F1BE-41F2-AE6E-B45008CF3896");
var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
var appSettings = Options.Create(new AppSettings
{
    DbConnections =
    [
        new DbConnection
        {
            DbName = "Rentall",
            ConnectionString = "Server=GESLER;Database=Rentall;Trusted_Connection=True;Encrypt=False;MultipleActiveResultSets=True;"
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

if (args.Contains("health-only", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine($"=== Office {officeIds} health-only ===");
    await PrintHealthAsync(health, orgId, officeIds);
    await PrintDepositIssuesAsync(health, orgId, officeIds);
    return;
}

if (args.Contains("noco-fix", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine("=== NoCo (office 5) BEFORE ===");
    await PrintHealthAsync(health, orgId, officeIds);
    var paymentFix = await manager.SyncJournalEntriesForHealthFixAsync(orgId, officeIds, "payment", [], (int)PaymentKind.Invoice, userId);
    Console.WriteLine($"Payments Errors={paymentFix.Errors.Count} Processed={paymentFix.DocumentsProcessed}");
    var depositFix = await manager.SyncJournalEntriesForHealthFixAsync(orgId, officeIds, "deposit", [], null, userId);
    Console.WriteLine($"Deposits Errors={depositFix.Errors.Count} Processed={depositFix.DocumentsProcessed}");
    foreach (var error in depositFix.Errors.Take(40))
        Console.WriteLine($"  DepositFix: {error}");
    await PrintHealthAsync(health, orgId, officeIds);
    await PrintDepositIssuesAsync(health, orgId, officeIds);
    return;
}

if (args.Contains("debug148", StringComparer.OrdinalIgnoreCase))
{
    var dp148 = Guid.Parse("7608E2C8-1734-42C8-A62E-44C5749FA6BF");
    var accounting = new AccountingRepository(appSettings, NullLogger<AccountingRepository>.Instance);
    var jeRepo = new JournalEntryRepository(appSettings, NullLogger<JournalEntryRepository>.Instance);
    var dep = await accounting.GetDepositByIdAsync(dp148, orgId);
    if (dep?.Splits == null)
    {
        Console.WriteLine("No deposit/splits");
        return;
    }

    foreach (var split in dep.Splits)
    {
        Console.WriteLine($"Split {split.DepositSplitId} Amount={split.Amount} Line={split.JournalEntryLineId}");
        if (split.JournalEntryLineId is not { } lineId || lineId == Guid.Empty)
            continue;
        var line = await jeRepo.GetJournalEntryLineByIdAsync(lineId);
        if (line == null) { Console.WriteLine("  line missing"); continue; }
        var je = await jeRepo.GetJournalEntryByIdAsync(line.JournalEntryId, orgId);
        Console.WriteLine($"  JE={je?.JournalEntryCode} PaymentId={je?.PaymentId} JeDeposit={je?.DepositCode}");
    }

    var py = (await accounting.GetPaymentsByOfficeIdsAsync(orgId, officeIds, (int)PaymentKind.Invoice))
        .FirstOrDefault(p => p.PaymentCode == "PY-000000912");
    Console.WriteLine($"PY-912 DepositCode={py?.DepositCode} DepositId={py?.DepositId}");

    var paymentIds = new HashSet<Guid>();
    foreach (var split in dep.Splits)
    {
        if (split.JournalEntryLineId is not { } lineId || lineId == Guid.Empty)
            continue;
        var line = await jeRepo.GetJournalEntryLineByIdAsync(lineId);
        if (line == null)
            continue;
        var je = await jeRepo.GetJournalEntryByIdAsync(line.JournalEntryId, orgId);
        if (je?.PaymentId is { } pid && pid != Guid.Empty)
            paymentIds.Add(pid);
    }

    Console.WriteLine($"PaymentIdsFromSplits={paymentIds.Count}");
    return;
}

if (args.Contains("stamp148", StringComparer.OrdinalIgnoreCase))
{
    var dp148 = Guid.Parse("7608E2C8-1734-42C8-A62E-44C5749FA6BF");
    var r = await manager.SyncJournalEntriesForHealthFixAsync(orgId, officeIds, "deposit", [dp148], null, userId);
    Console.WriteLine($"stamp148 Errors={r.Errors.Count} Processed={r.DocumentsProcessed} Skipped={r.JournalEntriesSkipped}");
    foreach (var error in r.Errors)
        Console.WriteLine($"  {error}");
    await PrintDepositIssuesAsync(health, orgId, officeIds);
    return;
}

if (args.Contains("deposits-only", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine("=== Deposits-only fix ===");
    var depositFix = await manager.SyncJournalEntriesForHealthFixAsync(orgId, officeIds, "deposit", [], null, userId);
    Console.WriteLine($"Deposits Errors={depositFix.Errors.Count} Processed={depositFix.DocumentsProcessed}");
    foreach (var error in depositFix.Errors.Take(40))
        Console.WriteLine($"  DepositFix: {error}");
    await PrintDepositIssuesAsync(health, orgId, officeIds);
    return;
}

if (args.Contains("tr047", StringComparer.OrdinalIgnoreCase))
{
    var trId = Guid.Parse("FCCBBFC7-D020-4DA2-A1D1-3443A005A084");
    var trFix = await manager.SyncJournalEntriesForHealthFixAsync(orgId, officeIds, "transfer", [trId], null, userId);
    Console.WriteLine($"TR-047 fix Errors={trFix.Errors.Count}");
    foreach (var error in trFix.Errors)
        Console.WriteLine($"  {error}");
    await PrintRowAsync("Transfers", await health.RunTransferHealthCheckAsync(orgId, officeIds));
    return;
}

if (args.Contains("tr022-fix", StringComparer.OrdinalIgnoreCase))
{
    var trId = Guid.Parse("247CC2AC-A092-437C-BB24-AA2FEE26CF1A");
    for (var attempt = 1; attempt <= 3; attempt++)
    {
        Console.WriteLine($"=== TR-022 fix attempt {attempt} ===");
        var trFix = await manager.SyncJournalEntriesForHealthFixAsync(orgId, officeIds, "transfer", [trId], null, userId);
        Console.WriteLine($"Errors={trFix.Errors.Count} Processed={trFix.DocumentsProcessed}");
        foreach (var error in trFix.Errors)
            Console.WriteLine($"  {error}");

        var transfers = await health.RunTransferHealthCheckAsync(orgId, officeIds);
        var r090 = transfers.Issues.Where(i =>
            (i.DocumentCode?.Contains("022", StringComparison.OrdinalIgnoreCase) ?? false)
            || (i.Detail?.Contains("090", StringComparison.OrdinalIgnoreCase) ?? false)
            || (i.RelatedCode?.Contains("090", StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        foreach (var issue in transfers.Issues.Where(i => i.DocumentCode == "TR-000000022"))
            Console.WriteLine($"  Issue: {issue.Issue} | {issue.Detail}");
        if (!transfers.Issues.Any(i => i.DocumentCode == "TR-000000022"))
        {
            Console.WriteLine("TR-022 transfer issues cleared.");
            break;
        }
    }

    return;
}

Console.WriteLine("=== SF (office 2) BEFORE ===");
await PrintHealthAsync(health, orgId, officeIds);

for (var pass = 1; pass <= 1; pass++)
{
    Console.WriteLine($"=== Pass {pass}: Fix Payments (Invoice) ===");
    var paymentFix = await manager.SyncJournalEntriesForHealthFixAsync(orgId, officeIds, "payment", [], (int)PaymentKind.Invoice, userId);
    Console.WriteLine($"Payments Errors={paymentFix.Errors.Count} Processed={paymentFix.DocumentsProcessed}");

    Console.WriteLine($"=== Pass {pass}: Fix Deposits ===");
    var depositFix = await manager.SyncJournalEntriesForHealthFixAsync(orgId, officeIds, "deposit", [], null, userId);
    Console.WriteLine($"Deposits Errors={depositFix.Errors.Count} Processed={depositFix.DocumentsProcessed}");
    foreach (var error in depositFix.Errors.Take(30))
        Console.WriteLine($"  DepositFix: {error}");

    Console.WriteLine($"=== Pass {pass}: Fix Transfers ===");
    var transferFix = await manager.SyncJournalEntriesForHealthFixAsync(orgId, officeIds, "transfer", [], null, userId);
    Console.WriteLine($"Transfers Errors={transferFix.Errors.Count} Processed={transferFix.DocumentsProcessed}");
    foreach (var error in transferFix.Errors)
        Console.WriteLine($"  TransferFix: {error}");

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

static async Task PrintDepositIssuesAsync(HealthRepository health, Guid orgId, string officeIds)
{
    var deposits = await health.RunDepositHealthCheckAsync(orgId, officeIds);
    Console.WriteLine($"Deposits Issues={deposits.Summary.DocumentsMissingJe}");
    foreach (var issue in deposits.Issues.Take(20))
        Console.WriteLine($"  {issue.DocumentCode} | {issue.Issue} | {issue.RelatedCode} | {issue.Amount} | {issue.Detail}");
}

file sealed class EnabledFeatureFlags : IFeatureFlagService
{
    public IReadOnlyDictionary<string, bool> GetAll() => new Dictionary<string, bool> { [FeatureFlagKeys.Accounting] = true };
    public bool IsEnabled(string f) => true;
    public Task<bool> IsEnabledAsync(string f, Guid o, CancellationToken c = default) => Task.FromResult(true);
    public void Set(string f, bool e) { }
}
