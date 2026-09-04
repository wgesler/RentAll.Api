using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Health;

namespace RentAll.Infrastructure.Repositories.Health;

public class HealthRepository : IHealthRepository
{
    private readonly string _dbConnectionString;

    public HealthRepository(IOptions<AppSettings> appSettings)
    {
        _dbConnectionString = appSettings.Value.DbConnections.Find(o => o.DbName.Equals("rentall", StringComparison.CurrentCultureIgnoreCase))!.ConnectionString;
    }

    public Task<DocumentHealthResult> RunReceiptHealthCheckAsync(Guid organizationId, string officeIds)
        => RunHealthCheckAsync("Maintenance.Receipt_HealthCheck", organizationId, officeIds);

    public Task<DocumentHealthResult> RunBillHealthCheckAsync(Guid organizationId, string officeIds)
        => RunHealthCheckAsync("Maintenance.Bill_HealthCheck", organizationId, officeIds);

    public Task<DocumentHealthResult> RunWorkOrderHealthCheckAsync(Guid organizationId, string officeIds)
        => RunHealthCheckAsync("Maintenance.WorkOrder_HealthCheck", organizationId, officeIds);

    public Task<DocumentHealthResult> RunInvoiceHealthCheckAsync(Guid organizationId, string officeIds)
        => RunHealthCheckAsync("Accounting.Invoice_HealthCheck", organizationId, officeIds);

    public Task<DocumentHealthResult> RunPaymentHealthCheckAsync(Guid organizationId, string officeIds, int? paymentKindId)
        => RunHealthCheckAsync(
            "Accounting.Payment_HealthCheck",
            organizationId,
            officeIds,
            new { OrganizationId = organizationId, OfficeIds = officeIds, PaymentKindId = paymentKindId });

    public Task<DocumentHealthResult> RunDepositHealthCheckAsync(Guid organizationId, string officeIds)
        => RunHealthCheckAsync("Accounting.Deposit_HealthCheck", organizationId, officeIds);

    public Task<DocumentHealthResult> RunTransferHealthCheckAsync(Guid organizationId, string officeIds)
        => RunHealthCheckAsync("Accounting.Transfer_HealthCheck", organizationId, officeIds);

    public Task<DocumentHealthResult> RunManualJournalEntryHealthCheckAsync(Guid organizationId, string officeIds)
        => RunHealthCheckAsync("Accounting.JournalEntry_HealthCheck", organizationId, officeIds);

    private async Task<DocumentHealthResult> RunHealthCheckAsync(
        string procedureName,
        Guid organizationId,
        string officeIds,
        object? parameters = null)
    {
        await using var db = new SqlConnection(_dbConnectionString);

        parameters ??= new { OrganizationId = organizationId, OfficeIds = officeIds };

        var (summaryRows, issueRows) = await db.DapperProcQueryMultipleAsync<DocumentHealthSummaryEntity, DocumentHealthIssueEntity>(
            procedureName,
            parameters);

        var summary = summaryRows?.FirstOrDefault();
        return new DocumentHealthResult
        {
            Summary = summary == null ? new DocumentHealthSummary() : MapSummary(summary),
            Issues = issueRows?.Select(MapIssue).ToList() ?? []
        };
    }

    private static DocumentHealthSummary MapSummary(DocumentHealthSummaryEntity entity)
        => new()
        {
            Section = entity.Section ?? string.Empty,
            DocumentType = entity.DocumentType ?? string.Empty,
            TotalDocuments = entity.TotalDocuments,
            DocumentsWithJe = entity.DocumentsWithJe,
            DocumentsMissingJe = entity.DocumentsMissingJe,
            DuplicateOpenJes = entity.DuplicateOpenJes,
            IsClean = entity.IsClean
        };

    private static DocumentHealthIssue MapIssue(DocumentHealthIssueEntity entity)
        => new()
        {
            Issue = entity.Issue ?? string.Empty,
            OrganizationId = entity.OrganizationId,
            OfficeId = entity.OfficeId,
            DocumentCode = entity.DocumentCode ?? string.Empty,
            DocumentId = entity.DocumentId,
            RelatedCode = entity.RelatedCode,
            RelatedId = entity.RelatedId,
            Amount = entity.Amount,
            TransactionDate = entity.TransactionDate,
            Detail = entity.Detail
        };
}
