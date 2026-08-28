using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private async Task CreateCrossOfficePaybackBillAsync(Receipt receipt, BankCard bankCard, Guid currentUser)
    {
        receipt = await LoadReceiptWithSplitsAsync(receipt);

        await CreateCrossOfficeOfficeBillAsync(receipt, receipt.OfficeId, bankCard.OfficeId, 1m, currentUser);
        await CreateCrossOfficeOfficeBillAsync(receipt, bankCard.OfficeId, receipt.OfficeId, -1m, currentUser);
    }

    private async Task CreateCrossOfficeOfficeBillAsync(Receipt receipt, int billOfficeId, int vendorOfficeId, decimal amountSign, Guid currentUser)
    {
        var vendorId = await ResolveInterOfficeVendorContactIdAsync(receipt.OrganizationId, billOfficeId, vendorOfficeId);
        var vendorName = await GetOfficeNameAsync(receipt.OrganizationId, vendorOfficeId);
        var receiptCode = receipt.ReceiptCode.Trim();

        var billCode = await _organizationManager.GenerateEntityCodeAsync(receipt.OrganizationId, EntityType.Receipt);
        if (string.IsNullOrWhiteSpace(billCode))
            throw new Exception("Unable to generate bill code for cross-office payback");

        var bill = new Receipt
        {
            OrganizationId = receipt.OrganizationId,
            OfficeId = billOfficeId,
            ReceiptCode = billCode.Trim(),
            PropertyIds = receipt.PropertyIds?.ToList() ?? new List<Guid>(),
            ReceiptDate = receipt.ReceiptDate,
            DueDate = receipt.DueDate == default ? receipt.ReceiptDate : receipt.DueDate,
            AccountingPeriod = receipt.AccountingPeriod,
            BillNumber = receiptCode,
            Amount = receipt.Amount * amountSign,
            Description = receipt.Description,
            BankCardId = null,
            VendorId = vendorId,
            VendorName = vendorName,
            PaidAmount = 0,
            Splits = CloneReceiptSplitsForCrossOfficePaybackBill(receipt.Splits, amountSign),
            ReceiptPath = receipt.ReceiptPath,
            IsUtility = receipt.IsUtility,
            BusinessPrivate = receipt.BusinessPrivate,
            IsActive = true,
            CreatedBy = currentUser
        };

        var createdBill = await _maintenanceRepository.CreateReceiptAsync(bill);
        await CreateJournalEntryFromBillAsync(createdBill, currentUser);
    }

    private static List<ReceiptSplit> CloneReceiptSplitsForCrossOfficePaybackBill(IEnumerable<ReceiptSplit>? splits, decimal amountSign)
    {
        return (splits ?? new List<ReceiptSplit>())
            .Select(split => new ReceiptSplit
            {
                Amount = split.Amount * amountSign,
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
