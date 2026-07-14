namespace RentAll.Api.Dtos.Accounting.ChartOfAccounts;

public class UpdateChartOfAccountDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public int AccountId { get; set; }
    public string AccountNo { get; set; } = string.Empty;
    public int AccountTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSubaccount { get; set; }
    public int? SubAccountId { get; set; }
    public string? Description { get; set; }
    public decimal? EndingBalance { get; set; }
    public DateOnly? StatementDate { get; set; }
    public string? Note { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(string currentOffices)
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (!currentOffices.Split(',', StringSplitOptions.RemoveEmptyEntries).Any(id => int.Parse(id) == OfficeId))
            return (false, "Unauthorized");

        if (AccountId < 0)
            return (false, "AccountId is required");

        if (string.IsNullOrWhiteSpace(AccountNo))
            return (false, "AccountNo is required");

        if (!Enum.IsDefined(typeof(AccountType), AccountTypeId))
            return (false, "Invalid AccountTypeId");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

        return CreateChartOfAccountDto.ValidateSubaccount(AccountId, IsSubaccount, SubAccountId);
    }

    public ChartOfAccount ToModel()
    {
        return new ChartOfAccount
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            AccountId = AccountId,
            AccountNo = AccountNo.Trim(),
            AccountType = (AccountType)AccountTypeId,
            Name = Name.Trim(),
            IsSubaccount = IsSubaccount,
            SubAccountId = IsSubaccount ? SubAccountId : null,
            Description = Description,
            EndingBalance = EndingBalance,
            StatementDate = StatementDate,
            Note = Note
        };
    }
}
