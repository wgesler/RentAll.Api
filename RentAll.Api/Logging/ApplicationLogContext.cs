namespace RentAll.Api.Logging;

public static class ApplicationLogContext
{
    public const string OrganizationIdKey = "ApplicationLog.OrganizationId";
    public const string OfficeIdKey = "ApplicationLog.OfficeId";

    public static void Set(HttpContext? httpContext, Guid organizationId, int? officeId = null)
    {
        if (httpContext == null)
            return;

        httpContext.Items[OrganizationIdKey] = organizationId;
        if (officeId.HasValue)
            httpContext.Items[OfficeIdKey] = officeId.Value;
    }
}
