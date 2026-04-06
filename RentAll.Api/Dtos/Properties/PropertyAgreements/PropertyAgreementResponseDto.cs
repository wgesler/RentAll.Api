using RentAll.Domain.Models;
using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Properties.PropertyAgreements;

public class PropertyAgreementResponseDto
{
    public Guid PropertyId { get; set; }
    public int OfficeId { get; set; }
    public string? W9Path { get; set; }
    public FileDetails? W9FileDetails { get; set; }
    public string? InsurancePath { get; set; }
    public DateTimeOffset? InsuranceExpiration { get; set; }
    public FileDetails? InsuranceFileDetails { get; set; }
    public string? AgreementPath { get; set; }
    public FileDetails? AgreementFileDetails { get; set; }
    public int Markup { get; set; }
    public int RevenueSplitOwner { get; set; }
    public int RevenueSplitOffice { get; set; }
    public decimal WorkingCapitalBalance { get; set; }
    public decimal LinenAndTowelFee { get; set; }
    public string? BankName { get; set; }
    public string? RoutingNumber { get; set; }
    public string? AccountNumber { get; set; }
    public string? Notes { get; set; }

    public PropertyAgreementResponseDto()
    {
    }

    public PropertyAgreementResponseDto(PropertyAgreement a)
    {
        PropertyId = a.PropertyId;
        OfficeId = a.OfficeId;
        W9Path = a.W9Path;
        InsurancePath = a.InsurancePath;
        InsuranceExpiration = a.InsuranceExpiration;
        AgreementPath = a.AgreementPath;
        Markup = a.Markup;
        RevenueSplitOwner = a.RevenueSplitOwner;
        RevenueSplitOffice = a.RevenueSplitOffice;
        WorkingCapitalBalance = a.WorkingCapitalBalance;
        LinenAndTowelFee = a.LinenAndTowelFee;
        BankName = a.BankName;
        RoutingNumber = a.RoutingNumber;
        AccountNumber = a.AccountNumber;
        Notes = a.Notes;
    }
}
