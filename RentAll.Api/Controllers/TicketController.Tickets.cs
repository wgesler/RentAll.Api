using RentAll.Api.Dtos.Tickets.Tickets;

namespace RentAll.Api.Controllers;

public partial class TicketController
{
    #region Get
    [HttpGet]
    public async Task<IActionResult> GetAllTickets()
    {
        try
        {
            var records = await _ticketRepository.GetTicketsByOfficeIdsAsync(CurrentOrganizationId, CurrentOfficeAccess);
            var response = records.Select(o => new TicketResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tickets");
            return ServerError("An error occurred while retrieving tickets");
        }
    }

    [HttpGet("property/{propertyId:guid}")]
    public async Task<IActionResult> GetTicketsByPropertyId(Guid propertyId)
    {
        if (propertyId == Guid.Empty)
            return BadRequest("PropertyId is required");

        try
        {
            var records = await _ticketRepository.GetTicketsByPropertyIdAsync(propertyId, CurrentOrganizationId, CurrentOfficeAccess);
            var response = records.Select(o => new TicketResponseDto(o));
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tickets for property: {PropertyId}", propertyId);
            return ServerError("An error occurred while retrieving tickets");
        }
    }

    [HttpGet("{ticketId:guid}")]
    public async Task<IActionResult> GetTicketById(Guid ticketId)
    {
        if (ticketId == Guid.Empty)
            return BadRequest("TicketId is required");

        try
        {
            var record = await _ticketRepository.GetTicketByIdAsync(ticketId, CurrentOrganizationId);
            if (record == null)
                return NotFound("Ticket record not found");

            return Ok(new TicketResponseDto(record));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ticket by ID: {TicketId}", ticketId);
            return ServerError("An error occurred while retrieving the ticket");
        }
    }
    #endregion

    #region Post
    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto dto)
    {
        if (dto == null)
            return BadRequest("Ticket data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            // Get a new Contact code
            var code = await _organizationManager.GenerateEntityCodeAsync(dto.OrganizationId, EntityType.Ticket);
            var ticket = dto.ToModel(code, CurrentUser);

            var created = await _ticketRepository.CreateTicketAsync(ticket);
            return Ok(new TicketResponseDto(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket");
            return ServerError("An error occurred while creating the ticket");
        }
    }
    #endregion

    #region Put
    [HttpPut]
    public async Task<IActionResult> UpdateTicket([FromBody] UpdateTicketDto dto)
    {
        if (dto == null)
            return BadRequest("Ticket data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid request data");

        try
        {
            var ticket = dto.ToModel(CurrentUser);
            var updated = await _ticketRepository.UpdateTicketAsync(ticket);
            if (updated.TicketStateType == TicketStateType.Closed)
                await _emailManager.AlertTicketListeners(updated);
            return Ok(new TicketResponseDto(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ticket: {TicketId}", dto.TicketId);
            return ServerError("An error occurred while updating the ticket");
        }
    }
    #endregion

    #region Delete
    [HttpDelete("{ticketId:guid}")]
    public async Task<IActionResult> DeleteTicketByIdAsync(Guid ticketId)
    {
        if (ticketId == Guid.Empty)
            return BadRequest("TicketId is required");

        try
        {
            await _ticketRepository.DeleteTicketByIdAsync(ticketId, CurrentOrganizationId, CurrentUser);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ticket: {TicketId}", ticketId);
            return ServerError("An error occurred while deleting the ticket");
        }
    }
    #endregion
}
