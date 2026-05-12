using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using RentAll.Api.Dtos.Tickets.Tickets;
using RentAll.Domain.Configuration;

namespace RentAll.Api.Controllers;

public partial class TicketController
{
    private static readonly Guid ExternalTicketSystemUserId = new("99999999-9999-9999-9999-999999999999");

    #region External Ticket Intake
    [AllowAnonymous]
    [HttpPost("external")]
    public async Task<IActionResult> CreateExternalTicket(
        [FromBody] CreateExternalTicketDto dto,
        [FromServices] IOptions<ExternalTicketIntakeSettings> settings)
    {
        if (dto == null)
            return BadRequest("Ticket data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        if (!IsExternalApiKeyValid(settings.Value.ApiKey))
            return Unauthorized("Invalid API key");

        try
        {
            var organization = await _organizationRepository.GetOrganizationByIdAsync(dto.OrganizationId);
            if (organization == null)
                return BadRequest("Invalid OrganizationId");

            var office = await _organizationRepository.GetOfficeByIdAsync(dto.OfficeId, dto.OrganizationId);
            if (office == null)
                return BadRequest("Invalid OfficeId for OrganizationId");

            var code = await _organizationManager.GenerateEntityCodeAsync(dto.OrganizationId, EntityType.Ticket);
            var ticket = dto.ToModel(code, ExternalTicketSystemUserId);

            var created = await _ticketRepository.CreateTicketAsync(ticket);
            return Ok(new TicketResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating external ticket intake request");
            return ServerError("An error occurred while creating the ticket");
        }
    }

    private bool IsExternalApiKeyValid(string configuredApiKey)
    {
        if (string.IsNullOrWhiteSpace(configuredApiKey))
            return false;

        var inboundApiKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(inboundApiKey))
            return false;

        return string.Equals(inboundApiKey.Trim(), configuredApiKey.Trim(), StringComparison.Ordinal);
    }
    #endregion
}
