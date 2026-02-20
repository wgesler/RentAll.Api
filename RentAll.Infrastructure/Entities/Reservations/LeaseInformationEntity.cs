namespace RentAll.Infrastructure.Entities.Reservations;

public class LeaseInformationEntity
{
    public Guid LeaseInformationId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? ContactId { get; set; }
    public string? RentalPayment { get; set; }
    public string? SecurityDeposit { get; set; }
    public string? SecurityDepositWaiver { get; set; }
    public string? CancellationPolicy { get; set; }
    public string? KeyPickUpDropOff { get; set; }
    public string? PartialMonth { get; set; }
    public string? DepartureNotification { get; set; }
    public string? Holdover { get; set; }
    public string? DepartureServiceFee { get; set; }
    public string? CheckoutProcedure { get; set; }
    public string? Parking { get; set; }
    public string? RulesAndRegulations { get; set; }
    public string? OccupyingTenants { get; set; }
    public string? UtilityAllowance { get; set; }
    public string? MaidService { get; set; }
    public string? Pets { get; set; }
    public string? Smoking { get; set; }
    public string? Emergencies { get; set; }
    public string? HomeownersAssociation { get; set; }
    public string? Indemnification { get; set; }
    public string? DefaultClause { get; set; }
    public string? AttorneyCollectionFees { get; set; }
    public string? ReservedRights { get; set; }
    public string? PropertyUse { get; set; }
    public string? Miscellaneous { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}

