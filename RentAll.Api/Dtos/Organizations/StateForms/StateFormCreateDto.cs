using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Organizations.StateForms;

public class StateFormCreateDto
{
    public string StateCode { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;
    public string? Path { get; set; }
    public FileDetails? FileDetails { get; set; }
    public string? FormAsHtml { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (string.IsNullOrWhiteSpace(StateCode) || StateCode.Trim().Length != 2)
            return (false, "State Code is required and must be 2 characters");

        if (string.IsNullOrWhiteSpace(FormName))
            return (false, "Form Name is required");

        return (true, null);
    }

    public StateForm ToModel(string organizationId)
    {
        return new StateForm
        {
            OrganizationId = organizationId,
            StateCode = StateCode.Trim().ToUpperInvariant(),
            FormName = FormName.Trim(),
            Path = string.IsNullOrWhiteSpace(Path) ? string.Empty : Path.Trim(),
            FormAsHtml = FormAsHtml
        };
    }
}
