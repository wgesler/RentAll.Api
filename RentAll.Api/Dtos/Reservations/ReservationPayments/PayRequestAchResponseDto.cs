using RentAll.Domain.Models.Accounting;

namespace RentAll.Api.Dtos.Reservations.ReservationPayments;

/// <summary>The result of saving a bank account for ACH Debit, shown on the pay-request page.</summary>
public class PayRequestAchResponseDto
{
    public bool Saved { get; set; }
    public long ConsentId { get; set; }
    public string? AccountLast4 { get; set; }
    public string? AccountType { get; set; }
    public string? NameOnAccount { get; set; }
    /// <summary>For the payer: what happened, in plain words.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>Number's code and text when it refused, so staff can look it up in the log.</summary>
    public int NumberCode { get; set; }
    public string? NumberMessage { get; set; }

    public PayRequestAchResponseDto(ContactBank saved)
    {
        Saved = true;
        ConsentId = saved.NumberConsentId ?? 0;
        AccountLast4 = saved.LastFour;
        AccountType = AccountTypeLabel((BankAccountType)saved.BankAccountTypeId);
        NameOnAccount = saved.NameOnAccount;
        Message = "Your bank account was saved.";
    }

    public PayRequestAchResponseDto(NumberAchConsentResult result)
    {
        Saved = false;
        ConsentId = result.ConsentId;
        NumberCode = result.ErrCode;
        NumberMessage = NumberResponseCodes.Readable(result.ErrMsg) ?? NumberResponseCodes.Readable(result.RespMsg);
        Message = string.IsNullOrWhiteSpace(NumberMessage)
            ? "Your bank account could not be saved. Please check the details and try again."
            : $"Your bank account could not be saved: {NumberMessage}";
    }

    public static string AccountTypeLabel(BankAccountType type) => type switch
    {
        BankAccountType.PersonalChecking => "Personal checking",
        BankAccountType.PersonalSavings => "Personal savings",
        BankAccountType.BusinessChecking => "Business checking",
        BankAccountType.BusinessSavings => "Business savings",
        _ => "Bank account"
    };
}
