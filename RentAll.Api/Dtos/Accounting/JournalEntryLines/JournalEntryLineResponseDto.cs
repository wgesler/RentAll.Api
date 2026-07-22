namespace RentAll.Api.Dtos.Accounting.JournalEntryLines;

using RentAll.Domain.Models;

public class JournalEntryLineResponseDto
{
    public Guid JournalEntryLineId { get; set; }
    public Guid JournalEntryId { get; set; }
    public int ChartOfAccountId { get; set; }
    public int? CostCodeId { get; set; }
    public Guid? PropertyId { get; set; }
    public string? PropertyCode { get; set; }
    public Guid? ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public Guid? ContactId { get; set; }
    public string? ContactName { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Memo { get; set; }
    public int PerspectiveId { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public JournalEntryLineResponseDto(JournalEntryLine line)
    {
        JournalEntryLineId = line.JournalEntryLineId;
        JournalEntryId = line.JournalEntryId;
        ChartOfAccountId = line.ChartOfAccountId;
        CostCodeId = line.CostCodeId;
        PropertyId = line.PropertyId;
        PropertyCode = line.PropertyCode;
        ReservationId = line.ReservationId;
        ReservationCode = line.ReservationCode;
        ContactId = line.ContactId;
        ContactName = line.ContactName;
        Debit = line.Debit;
        Credit = line.Credit;
        Memo = line.Memo;
        PerspectiveId = (int)line.PerspectiveId;
        CreatedOn = line.CreatedOn;
        CreatedBy = line.CreatedBy;
        ModifiedOn = line.ModifiedOn;
        ModifiedBy = line.ModifiedBy;
    }
}
