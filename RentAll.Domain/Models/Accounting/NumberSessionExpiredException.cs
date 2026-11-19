namespace RentAll.Domain.Models.Accounting;

/// <summary>
/// Number rejected the session key: 5030 (expired) or 5050 (the caller's IP changed since it authenticated).
/// Either way the fix is to authenticate again and retry once.
/// </summary>
public class NumberSessionExpiredException : InvalidOperationException
{
    public int ErrCode { get; }

    public NumberSessionExpiredException(int errCode, string? message)
        : base(string.IsNullOrWhiteSpace(message) ? $"Number session key was rejected ({errCode})." : message)
    {
        ErrCode = errCode;
    }

    public static bool IsSessionError(int errCode) => errCode is 5030 or 5050;
}
