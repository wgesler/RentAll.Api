namespace RentAll.Domain.Constants;

public static class SystemConstants
{
    public static readonly Guid SystemUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid SystemOrganizationId = SystemUserId;
    public const string SystemUserAccountEmail = "system@rentall.com";
    public const string SystemUserAccountName = "System User";
    public const string SystemUserOutboundEmail = "wendy.gesler@rentallexchange.com";
    public const string SystemUserOutboundName = "Wendy Gesler";
}
