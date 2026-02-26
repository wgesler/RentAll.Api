
namespace RentAll.Api.Controllers
{
    public partial class OrganizationController
    {

        #region Get

        /// <summary>
        /// Get all agents
        /// </summary>
        /// <returns>List of agents</returns>
        [HttpGet("agent")]
        public async Task<IActionResult> GetAllAgents()
        {
            try
            {
                var agents = await _organizationRepository.GetAllAgentsAsync(CurrentOrganizationId);
                var response = agents.Select(a => new AgentResponseDto(a));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all agents");
                return ServerError("An error occurred while retrieving agents");
            }
        }

        /// <summary>
        /// Get agent by ID
        /// </summary>
        /// <param name="agentId">Agent ID</param>
        /// <returns>Agent</returns>
        [HttpGet("agent/{agentId}")]
        public async Task<IActionResult> GetAgentById(Guid agentId)
        {
            if (agentId == Guid.Empty)
                return BadRequest("Agent ID is required");

            try
            {
                var agent = await _organizationRepository.GetAgentByIdAsync(agentId, CurrentOrganizationId);
                if (agent == null)
                    return NotFound("Agent not found");

                return Ok(new AgentResponseDto(agent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agent by ID: {AgentId}", agentId);
                return ServerError("An error occurred while retrieving the agent");
            }
        }

        /// <summary>
        /// Check if agent exists by code
        /// </summary>
        /// <param name="agentCode">Agent Code</param>
        /// <returns>Boolean indicating if agent exists</returns>
        [HttpGet("agent/exists/{agentCode}")]
        public async Task<IActionResult> ExistsByCode(string agentCode)
        {
            if (string.IsNullOrWhiteSpace(agentCode))
                return BadRequest("Agent Code is required");

            try
            {
                var exists = await _organizationRepository.ExistsAgentByCodeAsync(agentCode, CurrentOrganizationId);
                return Ok(new { exists = exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if agent exists by code: {AgentCode}", agentCode);
                return ServerError("An error occurred while checking agent existence");
            }
        }

        #endregion

        #region Post

        /// <summary>
        /// Create a new agent
        /// </summary>
        /// <param name="dto">Agent data</param>
        /// <returns>Created agent</returns>
        [HttpPost("agent")]
        public async Task<IActionResult> CreateAgent([FromBody] CreateAgentDto dto)
        {
            if (dto == null)
                return BadRequest("Agent data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Check if AgentCode already exists
                if (await _organizationRepository.ExistsAgentByCodeAsync(dto.AgentCode, CurrentOrganizationId))
                    return Conflict("Agent Code already exists");

                var agent = dto.ToModel(CurrentUser);
                var createdAgent = await _organizationRepository.CreateAgentAsync(agent);

                var response = new AgentResponseDto(createdAgent);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating agent");
                return ServerError("An error occurred while creating the agent");
            }
        }

        #endregion

        #region Put

        /// <summary>
        /// Update an existing agent
        /// </summary>
        /// <param name="dto">Agent data</param>
        /// <returns>Updated agent</returns>
        [HttpPut("agent")]
        public async Task<IActionResult> UpdateAgent([FromBody] UpdateAgentDto dto)
        {
            if (dto == null)
                return BadRequest("Agent data is required");

            var (isValid, errorMessage) = dto.IsValid();
            if (!isValid)
                return BadRequest(errorMessage ?? "Invalid request data");

            try
            {
                // Check if agent exists
                var existingAgent = await _organizationRepository.GetAgentByIdAsync(dto.AgentId, CurrentOrganizationId);
                if (existingAgent == null)
                    return NotFound("Agent not found");

                // Check if AgentCode is being changed and if the new code already exists
                if (existingAgent.AgentCode != dto.AgentCode)
                {
                    if (await _organizationRepository.ExistsAgentByCodeAsync(dto.AgentCode, CurrentOrganizationId))
                        return Conflict("Agent Code already exists");
                }

                var agent = dto.ToModel(CurrentUser);
                var updatedAgent = await _organizationRepository.UpdateAgentByIdAsync(agent);
                return Ok(new AgentResponseDto(updatedAgent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating agent: {AgentId}", dto.AgentId);
                return ServerError("An error occurred while updating the agent");
            }
        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete an agent
        /// </summary>
        /// <param name="agentId">Agent ID</param>
        /// <returns>No content</returns>
        [HttpDelete("agent/{agentId}")]
        public async Task<IActionResult> DeleteAgent(Guid agentId)
        {
            if (agentId == Guid.Empty)
                return BadRequest("Agent ID is required");

            try
            {
                // Check if agent exists
                var agent = await _organizationRepository.GetAgentByIdAsync(agentId, CurrentOrganizationId);
                if (agent == null)
                    return NotFound("Agent not found");

                await _organizationRepository.DeleteAgentByIdAsync(agentId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting agent: {AgentId}", agentId);
                return ServerError("An error occurred while deleting the agent");
            }
        }

        #endregion

    }
}
