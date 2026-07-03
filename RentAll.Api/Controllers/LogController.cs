using Microsoft.AspNetCore.Authorization;
using RentAll.Api.Dtos.Logs;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers
{
    [ApiController]
    [Route("api/log")]
    [Authorize]
    public class LogController : BaseController
    {
        private readonly ILoggingRepository _loggingRepository;

        public LogController(ILoggingRepository loggingRepository)
        {
            _loggingRepository = loggingRepository;
        }

        #region Accounting Error Log
        [HttpGet("accounting-error")]
        public async Task<IActionResult> GetAllAccountingError()
        {
            if (!HasAdminAccess())
                return Unauthorized("Only Admin or SuperAdmin can access logs.");

            var rows = await _loggingRepository.GetAllAccountingErrorsByOrganizationIdAsync(CurrentOrganizationId);
            var response = rows.Select(row => new AccountingErrorLogResponseDto(row)).ToList();
            return Ok(response);
        }

        [HttpGet("accounting-error/{accountingErrorId:guid}")]
        public async Task<IActionResult> GetAccountingErrorById(Guid accountingErrorId)
        {
            if (!HasAdminAccess())
                return Unauthorized("Only Admin or SuperAdmin can access logs.");

            var row = await _loggingRepository.GetAccountingErrorByIdAsync(accountingErrorId, CurrentOrganizationId);
            if (row == null)
                return NotFound("AccountingError record was not found.");

            return Ok(new AccountingErrorLogResponseDto(row));
        }

        [HttpDelete("accounting-error")]
        public async Task<IActionResult> DeleteAllAccountingError()
        {
            if (!HasAdminAccess())
                return Unauthorized("Only Admin or SuperAdmin can access logs.");

            await _loggingRepository.DeleteAllAccountingErrorsByOrganizationIdAsync(CurrentOrganizationId);
            return Ok();
        }
        #endregion

        #region Accounting Log
        [HttpGet("accounting-log")]
        public async Task<IActionResult> GetAllAccountingLog()
        {
            if (!HasAdminAccess())
                return Unauthorized("Only Admin or SuperAdmin can access logs.");

            var rows = await _loggingRepository.GetAllAccountingLogsByOrganizationIdAsync(CurrentOrganizationId);
            var response = rows.Select(row => new AccountingLogResponseDto(row)).ToList();
            return Ok(response);
        }

        [HttpGet("accounting-log/{id:int}")]
        public async Task<IActionResult> GetAccountingLogById(int id)
        {
            if (!HasAdminAccess())
                return Unauthorized("Only Admin or SuperAdmin can access logs.");

            var row = await _loggingRepository.GetAccountingLogByIdAsync(id, CurrentOrganizationId);
            if (row == null)
                return NotFound("AccountingLog record was not found.");

            return Ok(new AccountingLogResponseDto(row));
        }

        [HttpDelete("accounting-log")]
        public async Task<IActionResult> DeleteAllAccountingLog()
        {
            if (!HasAdminAccess())
                return Unauthorized("Only Admin or SuperAdmin can access logs.");

            await _loggingRepository.DeleteAllAccountingLogsByOrganizationIdAsync(CurrentOrganizationId);
            return Ok();
        }
        #endregion

        #region Application Log
        [HttpGet("application-log")]
        public async Task<IActionResult> GetAllApplicationLog()
        {
            if (!HasAdminAccess())
                return Unauthorized("Only Admin or SuperAdmin can access logs.");

            var rows = await _loggingRepository.GetAllApplicationLogsByOrganizationIdAsync(CurrentOrganizationId);
            var response = rows.Select(row => new ApplicationLogResponseDto(row)).ToList();
            return Ok(response);
        }

        [HttpGet("application-log/{id:int}")]
        public async Task<IActionResult> GetApplicationLogById(int id)
        {
            if (!HasAdminAccess())
                return Unauthorized("Only Admin or SuperAdmin can access logs.");

            var row = await _loggingRepository.GetApplicationLogByIdAsync(id, CurrentOrganizationId);
            if (row == null)
                return NotFound("ApplicationLog record was not found.");

            return Ok(new ApplicationLogResponseDto(row));
        }

        [HttpDelete("application-log")]
        public async Task<IActionResult> DeleteAllApplicationLog()
        {
            if (!HasAdminAccess())
                return Unauthorized("Only Admin or SuperAdmin can access logs.");

            await _loggingRepository.DeleteAllApplicationLogsByOrganizationIdAsync(CurrentOrganizationId);
            return Ok();
        }
        #endregion

        #region Database Error Log
        [HttpGet("database-error")]
        public async Task<IActionResult> GetAllDatabaseError()
        {
            if (!HasAdminAccess())
                return Unauthorized("Only Admin or SuperAdmin can access logs.");

            var rows = await _loggingRepository.GetAllDatabaseErrorLogsByOrganizationIdAsync(CurrentOrganizationId);
            var response = rows.Select(row => new DatabaseErrorLogResponseDto(row)).ToList();
            return Ok(response);
        }

        [HttpGet("database-error/{id:int}")]
        public async Task<IActionResult> GetDatabaseErrorById(int id)
        {
            if (!HasAdminAccess())
                return Unauthorized("Only Admin or SuperAdmin can access logs.");

            var row = await _loggingRepository.GetDatabaseErrorLogByIdAsync(id, CurrentOrganizationId);
            if (row == null)
                return NotFound("DatabaseError record was not found.");

            return Ok(new DatabaseErrorLogResponseDto(row));
        }

        [HttpDelete("database-error")]
        public async Task<IActionResult> DeleteAllDatabaseError()
        {
            if (!HasAdminAccess())
                return Unauthorized("Only Admin or SuperAdmin can access logs.");

            await _loggingRepository.DeleteAllDatabaseErrorLogsByOrganizationIdAsync(CurrentOrganizationId);
            return Ok();
        }
        #endregion

        #region General Error Log
        [HttpGet("general-error")]
        public async Task<IActionResult> GetAllGeneralError()
        {
            if (!HasAdminAccess())
                return Unauthorized("Only Admin or SuperAdmin can access logs.");

            var rows = await _loggingRepository.GetAllGeneralErrorLogsByOrganizationIdAsync(CurrentOrganizationId);
            var response = rows.Select(row => new GeneralErrorLogResponseDto(row)).ToList();
            return Ok(response);
        }

        [HttpGet("general-error/{id:int}")]
        public async Task<IActionResult> GetGeneralErrorById(int id)
        {
            if (!HasAdminAccess())
                return Unauthorized("Only Admin or SuperAdmin can access logs.");

            var row = await _loggingRepository.GetGeneralErrorLogByIdAsync(id, CurrentOrganizationId);
            if (row == null)
                return NotFound("GeneralError record was not found.");

            return Ok(new GeneralErrorLogResponseDto(row));
        }

        [HttpDelete("general-error")]
        public async Task<IActionResult> DeleteAllGeneralError()
        {
            if (!HasAdminAccess())
                return Unauthorized("Only Admin or SuperAdmin can access logs.");

            await _loggingRepository.DeleteAllGeneralErrorLogsByOrganizationIdAsync(CurrentOrganizationId);
            return Ok();
        }
        #endregion

        #region Utility
        private bool HasAdminAccess()
        {
            return IsAdmin() || IsSuperAdmin();
        }
        #endregion
    }
}
