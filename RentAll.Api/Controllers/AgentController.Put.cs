using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Agents;

namespace RentAll.Api.Controllers
{
    public partial class AgentController
    {
        /// <summary>
        /// Update an existing agent
        /// </summary>
        /// <param name="id">Agent ID</param>
        /// <param name="dto">Agent data</param>
        /// <returns>Updated agent</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAgentDto dto)
        {
            if (dto == null)
                return BadRequest("Agent data is required");

            var (isValid, errorMessage) = dto.IsValid(id);
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Check if agent exists
                var existingAgent = await _agentRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (existingAgent == null)
                    return NotFound("Agent not found");

                // Check if AgentCode is being changed and if the new code already exists
                if (existingAgent.AgentCode != dto.AgentCode)
                {
                    if (await _agentRepository.ExistsByAgentCodeAsync(dto.AgentCode, CurrentOrganizationId))
                        return Conflict("Agent Code already exists");
                }

                var agent = dto.ToModel(CurrentUser);
                var updatedAgent = await _agentRepository.UpdateByIdAsync(agent);
                return Ok(new AgentResponseDto(updatedAgent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating agent: {AgentId}", id);
                return ServerError("An error occurred while updating the agent");
            }
        }
    }
}

