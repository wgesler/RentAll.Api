using Microsoft.AspNetCore.Mvc;

namespace RentAll.Api.Controllers
{
	public partial class DocumentController
	{
		/// <summary>
		/// Delete a document (soft delete)
		/// </summary>
		/// <param name="id">Document ID</param>
		/// <returns>No content</returns>
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(Guid id)
		{
			if (id == Guid.Empty)
				return BadRequest(new { message = "Document ID is required" });

			try
			{
				var existing = await _documentRepository.GetByIdAsync(id, CurrentOrganizationId);
				if (existing == null || existing.IsDeleted)
					return NotFound(new { message = "Document not found" });

				await _documentRepository.DeleteByIdAsync(id, CurrentOrganizationId);
				return NoContent();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting document: {DocumentId}", id);
				return StatusCode(500, new { message = "An error occurred while deleting the document" });
			}
		}
	}
}

