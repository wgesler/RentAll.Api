namespace RentAll.Domain;

public static class EntityCodeFormatting
{
    public const int NumberDigits = 9;

    public static string Format(string prefix, int nextNumber)
        => $"{prefix}-{nextNumber.ToString($"D{NumberDigits}")}";

    public static string FormatContact(string prefix, int nextNumber)
        => $"C{prefix}-{nextNumber.ToString($"D{NumberDigits}")}";
}
