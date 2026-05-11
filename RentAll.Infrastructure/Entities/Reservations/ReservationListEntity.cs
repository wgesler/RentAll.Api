namespace RentAll.Infrastructure.Entities.Reservations;

public class ReservationListEntity
{
    public Guid ReservationId { get; set; }
    public string ReservationCode { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public string OfficeName { get; set; } = string.Empty;
    public string? AgentCode { get; set; }

    public Guid ContactId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string TenantName { get; set; } = string.Empty;

    public decimal MonthlyRate { get; set; }
    public decimal DailyRate { get; set; }
    public decimal BillingRate { get; set; }
    public int BillingTypeId { get; set; }
    public DateOnly ArrivalDate { get; set; }
    public DateOnly DepartureDate { get; set; }
    public int ReservationTypeId { get; set; }
    public int ReservationStatusId { get; set; }
    public bool HasPets { get; set; }
    public Guid? MaidUserId { get; set; }
    public DateOnly? MaidStartDate { get; set; }
    public int FrequencyId { get; set; }
    public decimal MaidServiceFee { get; set; }

    public int CurrentInvoiceNo { get; set; }

    public Guid? aCleanerUserId { get; set; }
    public DateOnly? aCleaningDate { get; set; }
    public Guid? aCarpetUserId { get; set; }
    public DateOnly? aCarpetDate { get; set; }
    public Guid? aInspectorUserId { get; set; }
    public DateOnly? aInspectingDate { get; set; }

    public Guid? dCleanerUserId { get; set; }
    public DateOnly? dCleaningDate { get; set; }
    public Guid? dCarpetUserId { get; set; }
    public DateOnly? dCarpetDate { get; set; }
    public Guid? dInspectorUserId { get; set; }
    public DateOnly? dInspectingDate { get; set; }

    public bool IsActive { get; set; }
    public DateTimeOffset CreatedOn { get; set; }

}
