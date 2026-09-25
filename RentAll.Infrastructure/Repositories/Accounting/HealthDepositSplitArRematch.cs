using Microsoft.Data.SqlClient;
using RentAll.Domain.Models;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.Entities.Accounting;

namespace RentAll.Infrastructure.Repositories.Accounting;

public partial class AccountingRepository
{
    public async Task<DepositSplitArRematchCandidates> GetDepositSplitArRematchCandidatesAsync(
        Guid organizationId,
        Guid depositId)
    {
        await using var db = new SqlConnection(_dbConnectionString);
        var (exact, stampPayments, noMatch) = await db.DapperProcQueryTripleAsync<
            DepositSplitArRematchMatchEntity,
            DepositSplitArRematchStampPaymentEntity,
            DepositSplitArRematchNoMatchEntity>(
            "Accounting.DepositSplit_ArRematchCandidates",
            new { OrganizationId = organizationId, DepositId = depositId });

        return new DepositSplitArRematchCandidates
        {
            ExactMatches = (exact ?? []).Select(MapArRematchMatch).ToList(),
            StampPaymentIds = (stampPayments ?? []).Select(row => row.PaymentId).Where(id => id != Guid.Empty).Distinct().ToList(),
            NoMatches = (noMatch ?? []).Select(MapArRematchNoMatch).ToList()
        };
    }

    private static DepositSplitArRematchMatch MapArRematchMatch(DepositSplitArRematchMatchEntity entity)
        => new()
        {
            DepositSplitId = entity.DepositSplitId,
            PaymentId = entity.PaymentId,
            ArJournalEntryLineId = entity.ArJournalEntryLineId,
            IsConsolidatedPaymentTotal = entity.IsConsolidatedPaymentTotal,
            CurrentLineId = entity.CurrentLineId,
            PaymentDepositId = entity.PaymentDepositId,
            PaymentCode = entity.PaymentCode,
            MatchOutcome = entity.MatchOutcome
        };

    private static DepositSplitArRematchNoMatch MapArRematchNoMatch(DepositSplitArRematchNoMatchEntity entity)
        => new()
        {
            DepositSplitId = entity.DepositSplitId,
            SplitDescription = entity.SplitDescription,
            MatchOutcome = entity.MatchOutcome,
            PaymentId = entity.PaymentId,
            PaymentCode = entity.PaymentCode,
            InvoiceCode = entity.InvoiceCode
        };
}
