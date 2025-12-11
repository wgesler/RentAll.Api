using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Agents;

namespace RentAll.Api.Controllers
{
    public partial class AgentController
    {
        /// <summary>
        /// Get all agents
        /// </summary>
        /// <returns>List of agents</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var agents = await _agentRepository.GetAllAsync();
                var response = agents.Select(a => new AgentResponseDto(a));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all agents");
                return StatusCode(500, new { message = "An error occurred while retrieving agents" });
            }
        }

        /// <summary>
        /// Get agent by ID
        /// </summary>
        /// <param name="id">Agent ID</param>
        /// <returns>Agent</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest(new { message = "Agent ID is required" });

            try
            {
                var agent = await _agentRepository.GetByIdAsync(id);
                if (agent == null)
                    return NotFound(new { message = "Agent not found" });

                return Ok(new AgentResponseDto(agent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agent by ID: {AgentId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the agent" });
            }
        }

        /// <summary>
        /// Check if agent exists by code
        /// </summary>
        /// <param name="agentCode">Agent Code</param>
        /// <returns>Boolean indicating if agent exists</returns>
        [HttpGet("exists/{agentCode}")]
        public async Task<IActionResult> ExistsByCode(string agentCode)
        {
            if (string.IsNullOrWhiteSpace(agentCode))
                return BadRequest(new { message = "Agent Code is required" });

            try
            {
                var exists = await _agentRepository.ExistsByAgentCodeAsync(agentCode);
                return Ok(new { exists = exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if agent exists by code: {AgentCode}", agentCode);
                return StatusCode(500, new { message = "An error occurred while checking agent existence" });
            }
        }
    }
}

