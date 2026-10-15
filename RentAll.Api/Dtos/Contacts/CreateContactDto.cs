using RentAll.Api.Dtos.Contacts.ContactCards;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Contacts;

public class CreateContactDto
{
    public int? OwnerLeadId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public List<int> OfficeAccess { get; set; } = new List<int>();
    public string? ExternalRefNo { get; set; }
    public int EntityTypeId { get; set; }
    public int OwnerTypeId { get; set; }
    public int VendorTypeId { get; set; }
    public List<string> Properties { get; set; } = new List<string>();
    public string? CompanyName { get; set; }
    public string? CompanyEmail { get; set; }
    public string? DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PreferredName { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? Extension { get; set; }
    public string Email { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Notes { get; set; }
    public bool IsInternational { get; set; }
    public FileDetails? W9FileDetails { get; set; }
    public int? FederalTaxClassificationId { get; set; }
    public int? OwnerIdTypeId { get; set; }
    public string? IdentificationNumber { get; set; }
    public int? OwnerDocumentId { get; set; }
    public FileDetails? DocumentFrontFileDetails { get; set; }
    public FileDetails? DocumentBackFileDetails { get; set; }
    public int? PropertyManagedBy { get; set; }
    public FileDetails? InsuranceFileDetails { get; set; }
    public DateOnly? InsuranceExpiration { get; set; }
    public int? Markup { get; set; }
    public decimal? RevenueSplitOwner { get; set; }
    public decimal? RevenueSplitOffice { get; set; }
    public decimal? WorkingCapitalBalance { get; set; }
    public decimal? LinenAndTowelFee { get; set; }
    public int? PaymentTermsId { get; set; }
    public int? ProrateTypeId { get; set; }
    public int? InvoiceMethodTypeId { get; set; }
    public string? BankName { get; set; }
    public string? RoutingNumber { get; set; }
    public string? AccountNumber { get; set; }
    public int? ContactCardId { get; set; }
    public UpsertContactCardDto? ContactCard { get; set; }
    public bool IsOwnerReady { get; set; }
    public bool IsActive { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OrganizationId == Guid.Empty)
            return (false, "OrganizationId is required");

        if (OfficeId <= 0)
            return (false, "OfficeId is required");

        if (EntityTypeId <= 0)
            return (false, "Entity Type ID is required");

        if (!string.IsNullOrWhiteSpace(ExternalRefNo) && ExternalRefNo.Trim().Length > 100)
            return (false, "ExternalRefNo must be 100 characters or fewer");

        if (string.IsNullOrWhiteSpace(Email))
            return (false, "Email is required");

        if (OfficeAccess == null || !OfficeAccess.Any())
            return (false, "OfficeAccess is required");

        // Validate enum values
        if (!Enum.IsDefined(typeof(EntityType), EntityTypeId))
            return (false, $"Invalid EntityType value: {EntityTypeId}");

        if (!Enum.IsDefined(typeof(OwnerType), OwnerTypeId))
            return (false, $"Invalid OwnerType value: {OwnerTypeId}");

        if (!Enum.IsDefined(typeof(VendorType), VendorTypeId))
            return (false, $"Invalid VendorType value: {VendorTypeId}");

        if (PaymentTermsId.HasValue && !Enum.IsDefined(typeof(TermType), PaymentTermsId.Value))
            return (false, $"Invalid PaymentTermsId value: {PaymentTermsId}");

        if (ProrateTypeId.HasValue && !Enum.IsDefined(typeof(ProrateType), ProrateTypeId.Value))
            return (false, $"Invalid ProrateTypeId value: {ProrateTypeId}");

        if (InvoiceMethodTypeId.HasValue && !Enum.IsDefined(typeof(InvoiceMethod), InvoiceMethodTypeId.Value))
            return (false, $"Invalid InvoiceMethodTypeId value: {InvoiceMethodTypeId}");

        if (FederalTaxClassificationId.HasValue && !Enum.IsDefined(typeof(FederalTaxType), FederalTaxClassificationId.Value))
            return (false, $"Invalid FederalTaxClassificationId value: {FederalTaxClassificationId}");

        if (OwnerIdTypeId.HasValue && !Enum.IsDefined(typeof(OwnerIdType), OwnerIdTypeId.Value))
            return (false, $"Invalid OwnerIdTypeId value: {OwnerIdTypeId}");

        if (OwnerDocumentId.HasValue && !Enum.IsDefined(typeof(OwnerDocumentType), OwnerDocumentId.Value))
            return (false, $"Invalid OwnerDocumentId value: {OwnerDocumentId}");

        if (PropertyManagedBy.HasValue && !Enum.IsDefined(typeof(PropertyManagerType), PropertyManagedBy.Value))
            return (false, $"Invalid PropertyManagedBy value: {PropertyManagedBy}");

        if (!string.IsNullOrWhiteSpace(IdentificationNumber) && IdentificationNumber.Trim().Length > 9)
            return (false, "IdentificationNumber must be 9 characters or fewer");

        if (ContactCard != null)
        {
            var (cardIsValid, cardError) = ContactCard.IsValid(requireCardNumber: true);
            if (!cardIsValid)
                return (false, cardError);
        }

        return (true, null);
    }

    public Contact ToModel(string code, Guid currentUser)
    {
        return new Contact
        {
            OrganizationId = OrganizationId,
            OwnerLeadId = OwnerLeadId,
            OfficeId = OfficeId,
            ContactCode = code,
            ExternalRefNo = string.IsNullOrWhiteSpace(ExternalRefNo) ? null : ExternalRefNo.Trim(),
            EntityType = (EntityType)EntityTypeId,
            OwnerType = (OwnerType)OwnerTypeId,
            VendorType = (VendorType)VendorTypeId,
            CompanyName = CompanyName,
            CompanyEmail = CompanyEmail,
            Properties = Properties ?? new List<string>(),
            DisplayName = DisplayName,
            FirstName = FirstName,
            LastName = LastName,
            PreferredName = PreferredName,
            OfficeAccess = OfficeAccess,
            Address1 = Address1,
            Address2 = Address2,
            City = City,
            State = State,
            Zip = Zip,
            DateOfBirth = DateOfBirth,
            Phone = Phone,
            Extension = Extension,
            Email = Email,
            Rating = Rating,
            Notes = Notes,
            IsInternational = IsInternational,
            W9Path = null, // Will be set by controller after file save
            FederalTaxClassification = FederalTaxClassificationId.HasValue ? (FederalTaxType)FederalTaxClassificationId.Value : null,
            OwnerIdType = OwnerIdTypeId.HasValue ? (OwnerIdType)OwnerIdTypeId.Value : null,
            IdentificationNumber = string.IsNullOrWhiteSpace(IdentificationNumber) ? null : IdentificationNumber.Trim(),
            OwnerDocument = OwnerDocumentId.HasValue ? (OwnerDocumentType)OwnerDocumentId.Value : null,
            DocumentFront = null,
            DocumentBack = null,
            PropertyManagedBy = PropertyManagedBy.HasValue ? (PropertyManagerType)PropertyManagedBy.Value : null,
            InsurancePath = null,// Will be set by controller after file save
            InsuranceExpiration = InsuranceExpiration,
            Markup = Markup,
            RevenueSplitOwner = RevenueSplitOwner,
            RevenueSplitOffice = RevenueSplitOffice,
            WorkingCapitalBalance = WorkingCapitalBalance,
            LinenAndTowelFee = LinenAndTowelFee,
            PaymentTerms = PaymentTermsId.HasValue ? (TermType)PaymentTermsId.Value : null,
            ProrateType = ProrateTypeId.HasValue ? (ProrateType)ProrateTypeId.Value : null,
            InvoiceMethod = InvoiceMethodTypeId.HasValue ? (InvoiceMethod)InvoiceMethodTypeId.Value : null,
            BankName = BankName,
            RoutingNumber = RoutingNumber,
            AccountNumber = AccountNumber,
            ContactCardId = ContactCardId,
            IsOwnerReady = IsOwnerReady,
            IsActive = IsActive,
            CreatedBy = currentUser
        };
    }
}
