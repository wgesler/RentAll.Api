using RentAll.Api.Dtos.Accounting.CheckPrint;
using RentAll.Domain.Enums;
using RentAll.Domain.Interfaces.Repositories;

namespace RentAll.Api.Controllers;

public partial class AccountingController
{
    #region Post

    [HttpPost("check-print/assign-numbers")]
    public async Task<IActionResult> AssignCheckNumbers([FromBody] AssignCheckNumbersDto dto)
    {
        if (dto == null)
            return BadRequest("Check print data is required");

        var (isValid, errorMessage) = dto.IsValid();
        if (!isValid)
            return BadRequest(errorMessage ?? "Invalid check print data");

        try
        {
            var accountingOffice = await _organizationRepository.GetAccountingOfficeByIdAsync(CurrentOrganizationId, dto.OfficeId);
            if (accountingOffice == null)
                return NotFound("Accounting office not found");

            var distinctJournalEntryIds = dto.JournalEntryIds.Distinct().ToList();
            var assignments = new List<CheckPrintAssignmentDto>();
            var nextCheckNumber = dto.StartingCheckNumber;

            foreach (var journalEntryId in distinctJournalEntryIds)
            {
                var journalEntry = await _journalEntryRepository.GetJournalEntryByIdAsync(journalEntryId, CurrentOrganizationId);
                if (journalEntry == null)
                    return BadRequest($"Journal entry not found: {journalEntryId}");

                if (journalEntry.OfficeId != dto.OfficeId)
                    return BadRequest($"Journal entry {journalEntryId} does not belong to office {dto.OfficeId}");

                if (journalEntry.SourceTypeId != (int)SourceType.BillPayment)
                    return BadRequest($"Journal entry {journalEntryId} is not a bill payment");

                if (!string.IsNullOrWhiteSpace(journalEntry.CheckNumber))
                    return BadRequest($"Journal entry {journalEntryId} already has check number {journalEntry.CheckNumber}");

                var checkNumber = nextCheckNumber.ToString();
                await _journalEntryRepository.UpdateJournalEntryCheckNumberByIdAsync(
                    journalEntryId,
                    CurrentOrganizationId,
                    checkNumber,
                    CurrentUser);

                assignments.Add(new CheckPrintAssignmentDto(journalEntryId, checkNumber));
                nextCheckNumber += 1;
            }

            await _organizationRepository.UpdateAccountingOfficeCheckNumberByIdAsync(
                CurrentOrganizationId,
                dto.OfficeId,
                nextCheckNumber,
                CurrentUser);

            return Ok(new AssignCheckNumbersResponseDto(assignments, nextCheckNumber));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning check numbers for office: {OfficeId}", dto.OfficeId);
            return ServerError("An error occurred while assigning check numbers");
        }
    }

    #endregion
}
