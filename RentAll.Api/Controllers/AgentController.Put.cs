using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Organizations.Agents;

namespace RentAll.Api.Controllers
{
    public partial class AgentController
    {
        /// <summary>
        /// Update an existing agent
        /// </summary>
        /// <param name="dto">Agent data</param>
        /// <returns>Updated agent</returns>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateAgentDto dto)
        {
            if (dto == null)
                return BadRequest("Agent data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Check if agent exists
                var existingAgent = await _officeRepository.GetAgentByIdAsync(dto.AgentId, CurrentOrganizationId);
                if (existingAgent == null)
                    return NotFound("Agent not found");

                // Check if AgentCode is being changed and if the new code already exists
                if (existingAgent.AgentCode != dto.AgentCode)
                {
                    if (await _officeRepository.ExistsAgentByCodeAsync(dto.AgentCode, CurrentOrganizationId))
                        return Conflict("Agent Code already exists");
                }

                var agent = dto.ToModel(CurrentUser);
                var updatedAgent = await _officeRepository.UpdateAgentByIdAsync(agent);
                return Ok(new AgentResponseDto(updatedAgent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating agent: {AgentId}", dto.AgentId);
                return ServerError("An error occurred while updating the agent");
            }
        }
    }
}

