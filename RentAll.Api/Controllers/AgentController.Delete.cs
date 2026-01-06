using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
    public partial class AgentController
    {
        /// <summary>
        /// Delete an agent
        /// </summary>
        /// <param name="id">Agent ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest("Agent ID is required");

            try
            {
                // Check if agent exists
                var agent = await _agentRepository.GetByIdAsync(id, CurrentOrganizationId);
                if (agent == null)
                    return NotFound("Agent not found");

                await _agentRepository.DeleteByIdAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting agent: {AgentId}", id);
                return ServerError("An error occurred while deleting the agent");
            }
        }
    }
}

