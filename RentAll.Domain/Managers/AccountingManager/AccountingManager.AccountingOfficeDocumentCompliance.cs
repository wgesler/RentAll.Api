using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    internal static int ApplyAccountingOfficeClosedPostingStatusComplianceToDocument(int currentPostingStatusId, PostingStatus? requiredStatus)
    {
        if (requiredStatus == null)
            return currentPostingStatusId;

        if (requiredStatus == PostingStatus.HardClosed)
            return (int)PostingStatus.HardClosed;

        if (currentPostingStatusId == (int)PostingStatus.HardClosed)
            return currentPostingStatusId;

        return (int)PostingStatus.SoftClosed;
    }

    private async Task<int> ResolveDocumentPostingStatusWithAccountingOfficeCloseComplianceAsync(Guid organizationId, int officeId, DateOnly transactionDate, DateOnly accountingPeriod, int currentPostingStatusId)
    {
        if (officeId <= 0)
            return currentPostingStatusId;

        var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(organizationId, officeId);
        if (accountingOffice == null)
            return currentPostingStatusId;

        var period = accountingPeriod == default && transactionDate != default
            ? FirstDayOfMonth(transactionDate)
            : accountingPeriod;

        var requiredStatus = ResolveRequiredPostingStatusForAccountingOfficeClose(transactionDate, period, accountingOffice);
        return ApplyAccountingOfficeClosedPostingStatusComplianceToDocument(currentPostingStatusId, requiredStatus);
    }

    private async Task EnsureInvoicePostingStatusComplianceAsync(Invoice invoice, Guid currentUser)
    {
        if (invoice.InvoiceId == Guid.Empty)
            return;

        var currentStatus = invoice.PostingStatusId ?? (int)PostingStatus.Open;
        var resolvedStatus = await ResolveDocumentPostingStatusWithAccountingOfficeCloseComplianceAsync(invoice.OrganizationId, invoice.OfficeId, invoice.InvoiceDate, invoice.AccountingPeriod, currentStatus);

        if (resolvedStatus == currentStatus)
            return;

        invoice.PostingStatusId = resolvedStatus;
        invoice.ModifiedBy = currentUser;
        await _accountingRepository.UpdateByIdAsync(invoice);
    }

    private async Task EnsureReceiptPostingStatusComplianceAsync(Receipt receipt, Guid currentUser)
    {
        if (receipt.ReceiptId == Guid.Empty)
            return;

        var currentStatus = receipt.PostingStatusId ?? (int)PostingStatus.Open;
        var resolvedStatus = await ResolveDocumentPostingStatusWithAccountingOfficeCloseComplianceAsync(receipt.OrganizationId, receipt.OfficeId, receipt.ReceiptDate, receipt.AccountingPeriod, currentStatus);

        if (resolvedStatus == currentStatus)
            return;

        receipt.PostingStatusId = resolvedStatus;
        receipt.ModifiedBy = currentUser;
        await _maintenanceRepository.UpdateReceiptAsync(receipt);
    }

    private async Task EnsureWorkOrderPostingStatusComplianceAsync(WorkOrder workOrder, Guid currentUser)
    {
        if (workOrder.WorkOrderId == Guid.Empty)
            return;

        var currentStatus = workOrder.PostingStatusId ?? (int)PostingStatus.Open;
        var resolvedStatus = await ResolveDocumentPostingStatusWithAccountingOfficeCloseComplianceAsync(workOrder.OrganizationId, workOrder.OfficeId, workOrder.WorkOrderDate, workOrder.AccountingPeriod, currentStatus);

        if (resolvedStatus == currentStatus)
            return;

        workOrder.PostingStatusId = resolvedStatus;
        workOrder.ModifiedBy = currentUser;
        await _maintenanceRepository.UpdateWorkOrderAsync(workOrder);
    }

    private async Task EnsurePaymentPostingStatusComplianceAsync(Payment payment, Guid currentUser)
    {
        if (payment.PaymentId == Guid.Empty)
            return;

        var currentStatus = payment.PostingStatusId ?? (int)PostingStatus.Open;
        var resolvedStatus = await ResolveDocumentPostingStatusWithAccountingOfficeCloseComplianceAsync(payment.OrganizationId, payment.OfficeId, payment.PaymentDate, default, currentStatus);

        if (resolvedStatus == currentStatus)
            return;

        payment.PostingStatusId = resolvedStatus;
        payment.ModifiedBy = currentUser;
        await _accountingRepository.UpdatePaymentAsync(payment);
    }

    private async Task EnsureDepositPostingStatusComplianceAsync(Deposit deposit, Guid currentUser)
    {
        if (deposit.DepositId == Guid.Empty)
            return;

        var currentStatus = deposit.PostingStatusId ?? (int)PostingStatus.Open;
        var resolvedStatus = await ResolveDocumentPostingStatusWithAccountingOfficeCloseComplianceAsync(deposit.OrganizationId, deposit.OfficeId, deposit.DepositDate, deposit.AccountingPeriod, currentStatus);

        if (resolvedStatus == currentStatus)
            return;

        deposit.PostingStatusId = resolvedStatus;
        deposit.ModifiedBy = currentUser;
        await _accountingRepository.UpdateDepositAsync(deposit);
    }

    private async Task EnsureTransferPostingStatusComplianceAsync(Transfer transfer, Guid currentUser)
    {
        if (transfer.TransferId == Guid.Empty)
            return;

        var currentStatus = transfer.PostingStatusId ?? (int)PostingStatus.Open;
        var resolvedStatus = await ResolveDocumentPostingStatusWithAccountingOfficeCloseComplianceAsync(transfer.OrganizationId, transfer.OfficeId, transfer.TransferDate, transfer.AccountingPeriod, currentStatus);

        if (resolvedStatus == currentStatus)
            return;

        transfer.PostingStatusId = resolvedStatus;
        transfer.ModifiedBy = currentUser;
        await _accountingRepository.UpdateTransferAsync(transfer);
    }
}
