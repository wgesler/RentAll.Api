using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Organizations.Agents;

namespace RentAll.Api.Controllers
{
    public partial class AgentController
    {
        /// <summary>
        /// Create a new agent
        /// </summary>
        /// <param name="dto">Agent data</param>
        /// <returns>Created agent</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAgentDto dto)
        {
            if (dto == null)
                return BadRequest("Agent data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Check if AgentCode already exists
                if (await _officeRepository.ExistsAgentByCodeAsync(dto.AgentCode, CurrentOrganizationId))
                    return Conflict("Agent Code already exists");

                var agent = dto.ToModel(CurrentUser);
                var createdAgent = await _officeRepository.CreateAgentAsync(agent);
                return CreatedAtAction(nameof(GetById), new { id = createdAgent.AgentId }, new AgentResponseDto(createdAgent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating agent");
                return ServerError("An error occurred while creating the agent");
            }
        }
    }
}







