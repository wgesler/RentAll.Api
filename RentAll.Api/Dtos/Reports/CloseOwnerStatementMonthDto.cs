using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Reports;

public class CloseOwnerStatementMonthDto
{
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public CloseOwnerStatementMonthLineDto[] Lines { get; set; } = [];

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (!EndDate.HasValue)
            return (false, "EndDate is required to close an owner statement month.");

        if (Lines == null || Lines.Length == 0)
            return (false, "At least one owner statement line is required.");

        if (Lines.Any(line => line == null))
            return (false, "Each owner statement line is required.");

        if (Lines.Any(line => line.OfficeId <= 0))
            return (false, "Each owner statement line must include a valid office.");

        if (Lines.Any(line => line.PropertyId == Guid.Empty))
            return (false, "Each owner statement line must include a valid property.");

        if (Lines.Any(line => string.IsNullOrWhiteSpace(line.PropertyCode)))
            return (false, "Each owner statement line must include a property code.");

        return (true, null);
    }

    public IReadOnlyList<OwnerStatementMonthCloseLine> ToLines()
    {
        return Lines.Select(line => new OwnerStatementMonthCloseLine
        {
            PropertyId = line.PropertyId,
            OfficeId = line.OfficeId,
            PropertyCode = line.PropertyCode.Trim(),
            OwnerId = line.OwnerId,
            OwnerNameLine = (line.OwnerNameLine ?? string.Empty).Trim(),
            ClosingBalance = line.ClosingBalance
        }).ToList();
    }
}

public class CloseOwnerStatementMonthLineDto
{
    public Guid PropertyId { get; set; }
    public int OfficeId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
    public string OwnerNameLine { get; set; } = string.Empty;
    public decimal ClosingBalance { get; set; }
}
