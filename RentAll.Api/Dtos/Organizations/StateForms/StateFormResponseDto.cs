using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Organizations.StateForms;

public class StateFormResponseDto
{
    public int StateFormId { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string StateCode { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public FileDetails? FileDetails { get; set; }
    public string? FormAsHtml { get; set; }

    public StateFormResponseDto(StateForm stateForm)
    {
        StateFormId = stateForm.StateFormId;
        OrganizationId = stateForm.OrganizationId;
        StateCode = stateForm.StateCode;
        FormName = stateForm.FormName;
        Path = stateForm.Path;
        FormAsHtml = stateForm.FormAsHtml;
    }
}
