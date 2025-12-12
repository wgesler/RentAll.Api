using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Agents;

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
                return BadRequest(new { message = "Agent data is required" });

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(new { message = errorMessage });

            try
            {
                // Check if AgentCode already exists
                if (await _agentRepository.ExistsByAgentCodeAsync(dto.AgentCode))
                    return Conflict(new { message = "Agent Code already exists" });

                var agent = dto.ToModel(dto, CurrentUser);
                var createdAgent = await _agentRepository.CreateAsync(agent);
                return CreatedAtAction(nameof(GetById), new { id = createdAgent.AgentId }, new AgentResponseDto(createdAgent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating agent");
                return StatusCode(500, new { message = "An error occurred while creating the agent" });
            }
        }
    }
}



