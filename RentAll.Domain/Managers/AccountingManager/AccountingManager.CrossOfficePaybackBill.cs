using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private async Task CreateCrossOfficePaybackBillAsync(Receipt receipt, BankCard bankCard, Guid currentUser)
    {
        receipt = await LoadReceiptWithSplitsAsync(receipt);

        var bankCardOfficeVendorId = await ResolveInterOfficeVendorContactIdAsync(receipt.OrganizationId, receipt.OfficeId, bankCard.OfficeId);
        var bankCardOfficeName = await GetOfficeNameAsync(receipt.OrganizationId, bankCard.OfficeId);
        var receiptCode = receipt.ReceiptCode.Trim();

        var billCode = await _organizationManager.GenerateEntityCodeAsync(receipt.OrganizationId, EntityType.Receipt);
        if (string.IsNullOrWhiteSpace(billCode))
            throw new Exception("Unable to generate bill code for cross-office payback");

        var bill = new Receipt
        {
            OrganizationId = receipt.OrganizationId,
            OfficeId = receipt.OfficeId,
            ReceiptCode = billCode.Trim(),
            PropertyIds = receipt.PropertyIds?.ToList() ?? new List<Guid>(),
            ReceiptDate = receipt.ReceiptDate,
            DueDate = receipt.DueDate == default ? receipt.ReceiptDate : receipt.DueDate,
            AccountingPeriod = receipt.AccountingPeriod,
            BillNumber = receiptCode,
            Amount = receipt.Amount,
            Description = receipt.Description,
            BankCardId = null,
            VendorId = bankCardOfficeVendorId,
            VendorName = bankCardOfficeName,
            PaidAmount = 0,
            Splits = CloneReceiptSplitsForCrossOfficePaybackBill(receipt.Splits),
            ReceiptPath = receipt.ReceiptPath,
            IsUtility = receipt.IsUtility,
            BusinessPrivate = receipt.BusinessPrivate,
            IsActive = true,
            CreatedBy = currentUser
        };

        var createdBill = await _maintenanceRepository.CreateReceiptAsync(bill);
        await CreateJournalEntryFromBillAsync(createdBill, currentUser);
    }

    private static List<ReceiptSplit> CloneReceiptSplitsForCrossOfficePaybackBill(IEnumerable<ReceiptSplit>? splits)
    {
        return (splits ?? new List<ReceiptSplit>())
            .Select(split => new ReceiptSplit
            {
                Amount = split.Amount,
                Description = split.Description,
                WorkOrder = split.WorkOrder,
                PropertyId = split.PropertyId,
                WorkOrderId = split.WorkOrderId,
                WorkOrderCode = split.WorkOrderCode,
                ReceiptTypeId = split.ReceiptTypeId,
                ChartOfAccountId = split.ChartOfAccountId,
                ChartOfAccountDisplayName = split.ChartOfAccountDisplayName
            })
            .ToList();
    }
}
