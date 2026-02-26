namespace RentAll.Api.Dtos.Organizations.Agents;

public class CreateAgentDto
{
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string AgentCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (string.IsNullOrWhiteSpace(AgentCode))
            return (false, "Agent Code is required");

        if (AgentCode.Length > 10)
            return (false, "Agent Code must be 10 characters or less");

        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Name is required");

        return (true, null);
    }

    public Agent ToModel(Guid currentUser)
    {
        return new Agent
        {
            AgentId = Guid.NewGuid(),
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            AgentCode = AgentCode,
            Name = Name,
            IsActive = IsActive,
            CreatedBy = currentUser
        };
    }
}

