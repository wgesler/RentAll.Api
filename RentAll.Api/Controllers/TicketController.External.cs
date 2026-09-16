using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Tickets.Tickets;

namespace RentAll.Api.Controllers;

public partial class TicketController
{
    #region Create
    [AllowAnonymous]
    [HttpPost("external")]
    public async Task<IActionResult> CreateExternalTicket([FromBody] CreateExternalTicketDto dto)
    {
        if (dto == null)
            return BadRequest("Ticket data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        var organization = await _organizationRepository.GetOrganizationByIdAsync(dto.OrganizationId);
        if (organization == null)
            return BadRequest("Invalid OrganizationId");

        if (!await _externalApiKeyService.IsApiKeyValidAsync(Request.Headers["X-Api-Key"].FirstOrDefault(), organization.GetExternalTicketKeyVaultSecretName()))
            return Unauthorized("Invalid API key");

        try
        {
            var office = await _organizationRepository.GetOfficeByIdAsync(dto.OfficeId, dto.OrganizationId);
            if (office == null)
                return BadRequest("Invalid OfficeId for OrganizationId");

            var code = await _organizationManager.GenerateEntityCodeAsync(dto.OrganizationId, EntityType.Ticket);
            var ticket = dto.ToModel(code, SystemUserId);

            var created = await _ticketRepository.CreateTicketAsync(ticket);
            return Ok(new TicketResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating external ticket intake request");
            return ServerError("An error occurred while creating the ticket");
        }
    }
    #endregion
}
