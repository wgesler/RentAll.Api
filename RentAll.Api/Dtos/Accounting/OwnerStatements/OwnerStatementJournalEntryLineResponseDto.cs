using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Accounting.OwnerStatements;

public class OwnerStatementJournalEntryLineResponseDto
{
    public Guid JournalEntryLineId { get; set; }
    public Guid JournalEntryId { get; set; }
    public string JournalEntryCode { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public int OfficeId { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int ChartOfAccountId { get; set; }
    public string AccountNo { get; set; } = string.Empty;
    public string ChartOfAccountName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public OwnerStatementJournalEntryLineResponseDto(OwnerStatementJournalEntryLine line)
    {
        JournalEntryLineId = line.JournalEntryLineId;
        JournalEntryId = line.JournalEntryId;
        JournalEntryCode = line.JournalEntryCode;
        TransactionDate = line.TransactionDate;
        OfficeId = line.OfficeId;
        PropertyId = line.PropertyId;
        PropertyCode = line.PropertyCode;
        ChartOfAccountId = line.ChartOfAccountId;
        AccountNo = line.AccountNo;
        ChartOfAccountName = line.ChartOfAccountName;
        Description = line.Description;
        Debit = line.Debit;
        Credit = line.Credit;
        Category = line.Category;
        Amount = line.Amount;
    }
}
