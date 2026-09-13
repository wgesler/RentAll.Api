namespace RentAll.Domain.Enums;

[Flags]
public enum ReceiptDraftSourceFlags
{
    None = 0,
    Upload = 1,
    StatementImport = 2
}
