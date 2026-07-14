using ReconcileModel = RentAll.Domain.Models.Reconcile;

namespace RentAll.Api.Dtos.Accounting.Reconcile;

public class ReconcileResponseDto
{
    public int ReconcileId { get; set; }
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

    public ReconcileResponseDto(ReconcileModel reconcile)
    {
        ReconcileId = reconcile.ReconcileId;
        AccountId = reconcile.AccountId;
        OrganizationId = reconcile.OrganizationId;
        OfficeId = reconcile.OfficeId;
        StatementDate = reconcile.StatementDate;
        EndingBalance = reconcile.EndingBalance;
        ServiceChargeAmount = reconcile.ServiceChargeAmount;
        ServiceChargeDate = reconcile.ServiceChargeDate;
        ServiceChargeAccountId = reconcile.ServiceChargeAccountId;
        InterestAmount = reconcile.InterestAmount;
        InterestDate = reconcile.InterestDate;
        InterestAccountId = reconcile.InterestAccountId;
    }
}

public class CreateReconcileDto
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

    public ReconcileModel ToModel(Guid organizationId)
    {
        return new ReconcileModel
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

public class UpdateReconcileDto
{
    public int ReconcileId { get; set; }
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
        if (ReconcileId <= 0)
            return (false, "ReconcileId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (!currentOffices.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == OfficeId))
            return (false, "Unauthorized");

        if (AccountId <= 0)
            return (false, "AccountId is required");

        return (true, null);
    }

    public ReconcileModel ToModel(Guid organizationId)
    {
        return new ReconcileModel
        {
            ReconcileId = ReconcileId,
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
