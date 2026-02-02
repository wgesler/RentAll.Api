using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Contacts;

public class CreateContactDto
{
	public Guid OrganizationId { get; set; }
	public int OfficeId { get; set; }
	public int EntityTypeId { get; set; }
	public Guid? EntityId { get; set; }
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string? Address1 { get; set; }
	public string? Address2 { get; set; }
	public string? City { get; set; }
	public string? State { get; set; }
	public string? Zip { get; set; }
	public string Phone { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string? Notes { get; set; }
	public bool IsActive { get; set; }

	public (bool IsValid, string? ErrorMessage) IsValid()
	{
		if (OrganizationId == Guid.Empty)
			return (false, "OrganizationId is required");

		if (OfficeId <= 0)
			return (false, "OfficeId is required");

		if (EntityTypeId <= 0)
			return (false, "Entity Type ID is required");

		if (string.IsNullOrWhiteSpace(FirstName))
			return (false, "First Name is required");

		if (string.IsNullOrWhiteSpace(LastName))
			return (false, "Last Name is required");

		if (string.IsNullOrWhiteSpace(Phone))
			return (false, "Phone is required");

		if (string.IsNullOrWhiteSpace(Email))
			return (false, "Email is required");

		// Validate enum value
		if (!Enum.IsDefined(typeof(EntityType), EntityTypeId))
			return (false, $"Invalid EntityType value: {EntityTypeId}");

		return (true, null);
	}

	public Contact ToModel(string code, Guid currentUser)
	{
		return new Contact
		{
			OrganizationId = OrganizationId,
			OfficeId = OfficeId,
			ContactCode = code,
			EntityType = (EntityType)EntityTypeId,
			EntityId = EntityId,
			FirstName = FirstName,
			LastName = LastName,
			Address1 = Address1,
			Address2 = Address2,
			City = City,
			State = State,
			Zip = Zip,
			Phone = Phone,
			Email = Email,
			Notes = Notes,
			IsActive = IsActive,
			CreatedBy = currentUser
		};
	}
}
