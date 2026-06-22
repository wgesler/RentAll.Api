using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private async Task<Receipt> LoadReceiptWithSplitsAsync(Receipt receipt)
    {
        if (receipt.ReceiptId == Guid.Empty)
            return receipt;

        return await _maintenanceRepository.GetReceiptByIdAsync(receipt.ReceiptId, receipt.OrganizationId)
            ?? receipt;
    }

    private static List<ReceiptSplit> ResolveDocumentSplitLines(Receipt document)
    {
        var allSplits = (document.Splits ?? new List<ReceiptSplit>())
            .OrderBy(s => s.ReceiptSplitId)
            .ToList();

        var nonZeroSplits = allSplits.Where(s => s.Amount != 0).ToList();
        if (nonZeroSplits.Count > 0)
            return nonZeroSplits;

        if (allSplits.Count > 0)
            return allSplits;

        if (document.Amount != 0)
        {
            var configuredChartOfAccountId = allSplits
                .Select(split => split.ChartOfAccountId)
                .FirstOrDefault(id => id is > 0);

            return
            [
                new ReceiptSplit
                {
                    Amount = document.Amount,
                    Description = document.Description,
                    ChartOfAccountId = configuredChartOfAccountId
                }
            ];
        }

        return allSplits;
    }
}
