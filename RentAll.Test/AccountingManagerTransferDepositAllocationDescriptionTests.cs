using RentAll.Domain.Enums;
using RentAll.Domain.Managers;
using RentAll.Domain.Models;

namespace RentAll.Test;

public class AccountingManagerTransferDepositAllocationDescriptionTests
{
    [Fact]
    public async Task ResolvePaymentBackedDepositSplitInvoiceSourceCode_Py953_UsesDominantInvoiceCode()
    {
        var manager = TransferSplitReconciliationTestSupport.CreateManager(out _);
        var split = new DepositSplit
        {
            JournalEntryLineId = TransferSplitReconciliationTestSupport.Payment953UfLineId,
            Amount = 4060m
        };

        var invoiceCode = await manager.ResolvePaymentBackedDepositSplitInvoiceSourceCodeAsync(
            TransferSplitReconciliationTestSupport.OrganizationId,
            split);

        Assert.Equal("R-000000396-002", invoiceCode);
    }

    [Fact]
    public async Task ResolveTransferDepositAllocations_Py953Payment_SplitsOwnerSdwAndBusiness()
    {
        var manager = TransferSplitReconciliationTestSupport.CreateManager(out _);

        var allocations = await manager.ResolveTransferDepositAllocationsAsync(
            TransferSplitReconciliationTestSupport.OrganizationId,
            TransferSplitReconciliationTestSupport.OfficeId,
            [
                new TransferDepositAllocationRequestItem
                {
                    DepositId = TransferSplitReconciliationTestSupport.Deposit156Id,
                    EscrowAmount = 4060m,
                    DepositSplitId = 1
                }
            ]);

        var allocation = Assert.Single(allocations);
        Assert.Equal(2808.40m, allocation.OwnerEscrow);
        Assert.Equal(48m, allocation.Sdw);
        Assert.Equal(1203.60m, allocation.Business);
        Assert.Equal("R-000000396-002", allocation.Description);
    }
}
