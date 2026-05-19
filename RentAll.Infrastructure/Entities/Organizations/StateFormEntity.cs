namespace RentAll.Infrastructure.Entities.Organizations;

public class StateFormEntity
{
    public int StateFormId { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string StateCode { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? FormAsHtml { get; set; }
}
