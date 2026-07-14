namespace RentAll.Api.Dtos.Accounting.ChartOfAccounts;

public class CreateChartOfAccountDto
{
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
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

        if (string.IsNullOrWhiteSpace(AccountNo))
            return (false, "AccountNo is required");

        if (!Enum.IsDefined(typeof(AccountType), AccountTypeId))
            return (false, "Invalid AccountTypeId");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

        return ValidateSubaccount(IsSubaccount, SubAccountId);
    }

    public ChartOfAccount ToModel()
    {
        return new ChartOfAccount
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
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

    internal static (bool IsValid, string? ErrorMessage) ValidateSubaccount(bool isSubaccount, int? subAccountId)
    {
        if (isSubaccount)
        {
            if (!subAccountId.HasValue)
                return (false, "SubAccountId is required when IsSubaccount is true");
        }
        else if (subAccountId.HasValue)
        {
            return (false, "SubAccountId must be null when IsSubaccount is false");
        }

        return (true, null);
    }

    internal static (bool IsValid, string? ErrorMessage) ValidateSubaccount(int accountId, bool isSubaccount, int? subAccountId)
    {
        var result = ValidateSubaccount(isSubaccount, subAccountId);
        if (!result.IsValid)
            return result;

        if (isSubaccount && subAccountId!.Value == accountId)
            return (false, "SubAccountId cannot equal AccountId");

        return (true, null);
    }
}
