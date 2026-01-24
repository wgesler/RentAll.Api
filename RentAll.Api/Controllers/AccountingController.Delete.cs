using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
	public partial class AccountingController
	{
		#region Invoice DELETE Endpoints

		/// <summary>
		/// Delete an invoice
		/// </summary>
		/// <param name="invoiceId">Invoice ID</param>
		/// <returns>No content</returns>
		[HttpDelete("invoice/{invoiceId}")]
		public async Task<IActionResult> DeleteInvoice(Guid invoiceId)
		{
			if (invoiceId == Guid.Empty)
				return BadRequest("Invoice ID is required");

			try
			{
				var existingInvoice = await _invoiceRepository.GetByIdAsync(invoiceId, CurrentOrganizationId);
				if (existingInvoice == null)
					return NotFound("Invoice not found");

				await _invoiceRepository.DeleteByIdAsync(invoiceId, CurrentOrganizationId);
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting invoice: {InvoiceId}", invoiceId);
				return ServerError("An error occurred while deleting the invoice");
			}
		}

		#endregion
	}
}
