using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.LeaseInformations;

public class LeaseInformationResponseDto
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

    public LeaseInformationResponseDto(LeaseInformation leaseInformation)
    {
        LeaseInformationId = leaseInformation.LeaseInformationId;
        PropertyId = leaseInformation.PropertyId;
        OrganizationId = leaseInformation.OrganizationId;
        ContactId = leaseInformation.ContactId;
        RentalPayment = leaseInformation.RentalPayment;
        SecurityDeposit = leaseInformation.SecurityDeposit;
        SecurityDepositWaiver = leaseInformation.SecurityDepositWaiver;
        CancellationPolicy = leaseInformation.CancellationPolicy;
        KeyPickUpDropOff = leaseInformation.KeyPickUpDropOff;
        PartialMonth = leaseInformation.PartialMonth;
        DepartureNotification = leaseInformation.DepartureNotification;
        Holdover = leaseInformation.Holdover;
        DepartureServiceFee = leaseInformation.DepartureServiceFee;
        CheckoutProcedure = leaseInformation.CheckoutProcedure;
        Parking = leaseInformation.Parking;
        RulesAndRegulations = leaseInformation.RulesAndRegulations;
        OccupyingTenants = leaseInformation.OccupyingTenants;
        UtilityAllowance = leaseInformation.UtilityAllowance;
        MaidService = leaseInformation.MaidService;
        Pets = leaseInformation.Pets;
        Smoking = leaseInformation.Smoking;
        Emergencies = leaseInformation.Emergencies;
        HomeownersAssociation = leaseInformation.HomeownersAssociation;
        Indemnification = leaseInformation.Indemnification;
        DefaultClause = leaseInformation.DefaultClause;
        AttorneyCollectionFees = leaseInformation.AttorneyCollectionFees;
        ReservedRights = leaseInformation.ReservedRights;
        PropertyUse = leaseInformation.PropertyUse;
        Miscellaneous = leaseInformation.Miscellaneous;
    }
}

