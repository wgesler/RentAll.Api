namespace RentAll.Api.Controllers
{
    public partial class CommonController
    {
        /// <summary>
        /// Get all states
        /// </summary>        
        /// <returns>List of states with two-digit code and name</returns>
        [HttpGet("state")]
        public async Task<IActionResult> GetStates()
        {
            try
            {
                var states = await _commonRepository.GetAllStatesAsync();
                var response = states.Select(s => new StateResponseDto(s));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting states");
                return ServerError("An error occurred while retrieving states");
            }
        }
    }
}
