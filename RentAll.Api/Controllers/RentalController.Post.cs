using Microsoft.AspNetCore.Mvc;
using RentAll.Api.Dtos.Rentals;

namespace RentAll.Api.Controllers
{
	public partial class RentalController
	{
		/// <summary>
		/// Create a new rental
		/// </summary>
		/// <param name="dto">Rental data</param>
		/// <returns>Created rental</returns>
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreateRentalDto dto)
		{
			if (dto == null)
				return BadRequest(new { message = "Rental data is required" });

			var (isValid, errorMessage) = dto.IsValid();
			if (!isValid)
				return BadRequest(new { message = errorMessage });

			try
			{
				var rental = dto.ToModel(CurrentUser);
				var createdRental = await _rentalRepository.CreateAsync(rental);
				return CreatedAtAction(nameof(GetById), new { id = createdRental.RentalId }, new RentalResponseDto(createdRental));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating rental");
				return StatusCode(500, new { message = "An error occurred while creating the rental" });
			}
		}
	}
}

