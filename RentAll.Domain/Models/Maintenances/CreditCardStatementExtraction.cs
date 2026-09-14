namespace RentAll.Domain.Models.Maintenances;

public class CreditCardStatementExtraction
{
    public string? StatementCardLastFour { get; set; }
    public int? StatementCardTypeId { get; set; }
    public string FullText { get; set; } = string.Empty;
    public IReadOnlyList<CreditCardStatementLine> Lines { get; set; } = Array.Empty<CreditCardStatementLine>();
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
}
