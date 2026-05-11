using Microsoft.AspNetCore.Authorization;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Models.Leads;

namespace RentAll.Api.Controllers;

[ApiController]
[Route("api/leads")]
[Authorize]
public partial class LeadController : BaseController
{
    private readonly ILeadRepository _leadRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ILogger<LeadController> _logger;

    public LeadController(
        ILeadRepository leadRepository,
        IOrganizationRepository organizationRepository,
        ILogger<LeadController> logger)
    {
        _leadRepository = leadRepository;
        _organizationRepository = organizationRepository;
        _logger = logger;
    }

    private async Task<IReadOnlySet<Guid>> GetAgentIdsForCurrentOrganizationAsync()
    {
        var agents = await _organizationRepository.GetAgentsByOrganizationIdAsync(CurrentOrganizationId);
        return agents.Select(a => a.AgentId).ToHashSet();
    }

    private async Task<bool> CanViewRentalLeadAsync(LeadRental rental)
    {
        if (!rental.AgentId.HasValue)
            return true;

        var agent = await _organizationRepository.GetAgentByIdAsync(rental.AgentId.Value, CurrentOrganizationId);
        return agent != null;
    }

    private async Task<bool> CanViewOwnerLeadAsync(LeadOwner owner)
    {
        if (!owner.AgentId.HasValue)
            return true;

        var agent = await _organizationRepository.GetAgentByIdAsync(owner.AgentId.Value, CurrentOrganizationId);
        return agent != null;
    }

    private async Task<bool> CanAssignAgentAsync(Guid? agentId)
    {
        if (!agentId.HasValue)
            return true;

        var agent = await _organizationRepository.GetAgentByIdAsync(agentId.Value, CurrentOrganizationId);
        return agent != null;
    }
}
