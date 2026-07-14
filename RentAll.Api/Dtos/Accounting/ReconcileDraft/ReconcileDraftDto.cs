using ReconcileDraftModel = RentAll.Domain.Models.ReconcileDraft;

namespace RentAll.Api.Dtos.Accounting.ReconcileDraft;

public class ReconcileDraftResponseDto
{
    public int AccountId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public DateOnly? StatementDate { get; set; }
    public decimal? EndingBalance { get; set; }
    public decimal? ServiceChargeAmount { get; set; }
    public DateOnly? ServiceChargeDate { get; set; }
    public int? ServiceChargeAccountId { get; set; }
    public decimal? InterestAmount { get; set; }
    public DateOnly? InterestDate { get; set; }
    public int? InterestAccountId { get; set; }

    public ReconcileDraftResponseDto(ReconcileDraftModel reconcileDraft)
    {
        AccountId = reconcileDraft.AccountId;
        OrganizationId = reconcileDraft.OrganizationId;
        OfficeId = reconcileDraft.OfficeId;
        StatementDate = reconcileDraft.StatementDate;
        EndingBalance = reconcileDraft.EndingBalance;
        ServiceChargeAmount = reconcileDraft.ServiceChargeAmount;
        ServiceChargeDate = reconcileDraft.ServiceChargeDate;
        ServiceChargeAccountId = reconcileDraft.ServiceChargeAccountId;
        InterestAmount = reconcileDraft.InterestAmount;
        InterestDate = reconcileDraft.InterestDate;
        InterestAccountId = reconcileDraft.InterestAccountId;
    }
}

public class SaveReconcileDraftDto
{
    public int OfficeId { get; set; }
    public int AccountId { get; set; }
    public DateOnly? StatementDate { get; set; }
    public decimal? EndingBalance { get; set; }
    public decimal? ServiceChargeAmount { get; set; }
    public DateOnly? ServiceChargeDate { get; set; }
    public int? ServiceChargeAccountId { get; set; }
    public decimal? InterestAmount { get; set; }
    public DateOnly? InterestDate { get; set; }
    public int? InterestAccountId { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(string currentOffices)
    {
        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (!currentOffices.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == OfficeId))
            return (false, "Unauthorized");

        if (AccountId <= 0)
            return (false, "AccountId is required");

        return (true, null);
    }

    public ReconcileDraftModel ToModel(Guid organizationId)
    {
        return new ReconcileDraftModel
        {
            OrganizationId = organizationId,
            OfficeId = OfficeId,
            AccountId = AccountId,
            StatementDate = StatementDate,
            EndingBalance = EndingBalance,
            ServiceChargeAmount = ServiceChargeAmount,
            ServiceChargeDate = ServiceChargeDate,
            ServiceChargeAccountId = ServiceChargeAccountId,
            InterestAmount = InterestAmount,
            InterestDate = InterestDate,
            InterestAccountId = InterestAccountId
        };
    }
}
