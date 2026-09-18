using RentAll.Api.Dtos.Maintenances.Receipts;
using RentAll.Domain.Constants;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models.Maintenances;
using RentAll.Infrastructure.Services;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RentAll.Api.Services;

public class CreditReportService
{
    private readonly IDocumentIntelligenceService _documentIntelligenceService;
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationManager _organizationManager;
    private readonly IAccountingRepository _accountingRepository;
    private readonly IContactRepository _contactRepository;
    private readonly ILogger<CreditReportService> _logger;

    public CreditReportService(IDocumentIntelligenceService documentIntelligenceService, IMaintenanceRepository maintenanceRepository, IOrganizationRepository organizationRepository, IOrganizationManager organizationManager, IAccountingRepository accountingRepository, IContactRepository contactRepository, ILogger<CreditReportService> logger)
    {
        _documentIntelligenceService = documentIntelligenceService;
        _maintenanceRepository = maintenanceRepository;
        _organizationRepository = organizationRepository;
        _organizationManager = organizationManager;
        _accountingRepository = accountingRepository;
        _contactRepository = contactRepository;
        _logger = logger;
    }

    public async Task<CreditReportResponseDto> ProcessAsync(CreditReportRequestDto dto, Guid currentUser, CancellationToken cancellationToken = default)
    {
        var fileBytes = ReceiptDocumentExtractionHelper.GetFileBytes(dto.FileDetails!);
        var contentType = string.IsNullOrWhiteSpace(dto.FileDetails!.ContentType) ? "application/octet-stream" : dto.FileDetails.ContentType;
        var fileName = dto.FileDetails.FileName;
        var extraction = await _documentIntelligenceService.ExtractCreditCardStatementAsync(fileBytes, contentType, fileName, cancellationToken);
        var warnings = extraction.Warnings.ToList();
        var response = new CreditReportResponseDto { FileName = fileName, Warnings = warnings };

        _logger.LogError("[CreditReportTrace] Step=Extracted LineCount={LineCount} OfficeId={OfficeId}", extraction.Lines.Count, dto.OfficeId);

        if (extraction.Lines.Count == 0)
            return response;

        var offices = (await _organizationRepository.GetOfficesByOrganizationIdAsync(dto.OrganizationId)).ToList();
        var officeIds = dto.OfficeId is > 0 ? dto.OfficeId.Value.ToString() : string.Join(',', offices.Select(office => office.OfficeId).Where(id => id > 0));
        var bankCards = string.IsNullOrWhiteSpace(officeIds) ? [] : (await _accountingRepository.GetBankCardsByOfficeIdsAsync(dto.OrganizationId, officeIds)).ToList();
        var vendors = (await _contactRepository.GetContactsByOrganizationIdAsync(dto.OrganizationId)).Where(contact => contact.EntityType == EntityType.Vendor && contact.IsActive).ToList();
        var dateRange = ResolveDateRange(extraction.Lines);
        var receipts = (await _maintenanceRepository.GetReceiptsByCriteriaAsync(new ReceiptGetCriteria
        {
            OrganizationId = dto.OrganizationId,
            OfficeIds = officeIds,
            IsActive = true,
            IncludeInactive = false,
            StartDate = dateRange.StartDate,
            EndDate = dateRange.EndDate
        })).Where(receipt => receipt.IsActive).ToList();
        var drafts = (await _maintenanceRepository.GetReceiptDraftsByCriteriaAsync(new ReceiptDraftGetCriteria
        {
            OrganizationId = dto.OrganizationId,
            OfficeIds = officeIds,
            IsActive = true,
            IncludeInactive = false,
            IncludePromoted = false,
            StartDate = dateRange.StartDate,
            EndDate = dateRange.EndDate
        })).Where(draft => draft.IsActive).ToList();

        var usedReceiptIds = new HashSet<Guid>();
        var usedDraftIds = new HashSet<Guid>();
        foreach (var line in extraction.Lines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var resolvedVendor = ResolveVendor(line.VendorName, vendors);
            var statementLastFour = line.CardLastFour ?? extraction.StatementCardLastFour;
            var resolvedCard = ResolveBankCard(statementLastFour, line.CardTypeId ?? extraction.StatementCardTypeId, bankCards);
            var matchedReceipt = receipts.FirstOrDefault(receipt => !usedReceiptIds.Contains(receipt.ReceiptId) && IsExactMatch(line, receipt.ReceiptDate, receipt.Amount, receipt.VendorId, receipt.VendorName, receipt.BankCardId, LookupLastFour(receipt.BankCardId, bankCards) ?? receipt.BankCardDisplayName, resolvedVendor?.ContactId, resolvedCard?.BankCardId, statementLastFour));
            if (matchedReceipt != null)
            {
                usedReceiptIds.Add(matchedReceipt.ReceiptId);
                response.CompleteMatches.Add(CreditReportResponseDto.FromLine(line, matchedReceipt, resolvedCard: resolvedCard));
                continue;
            }

            var matchedDraft = drafts.FirstOrDefault(draft => !usedDraftIds.Contains(draft.ReceiptDraftId) && draft.ReceiptDate.HasValue && IsExactMatch(line, draft.ReceiptDate.Value, draft.Amount, draft.VendorId, draft.VendorName, draft.BankCardId, LookupLastFour(draft.BankCardId, bankCards) ?? draft.BankCardDisplayName, resolvedVendor?.ContactId, resolvedCard?.BankCardId, statementLastFour));
            if (matchedDraft != null)
            {
                usedDraftIds.Add(matchedDraft.ReceiptDraftId);
                matchedDraft = await EnrichMatchedDraftAsync(matchedDraft, line, resolvedVendor, resolvedCard, dto.OfficeId, currentUser);
                response.DraftMatches.Add(CreditReportResponseDto.FromLine(line, draft: matchedDraft, resolvedCard: resolvedCard));
                continue;
            }

            response.CreatedDrafts.Add(CreditReportResponseDto.FromLine(line, vendorId: resolvedVendor?.ContactId, bankCardId: resolvedCard?.BankCardId, cardTypeId: line.CardTypeId ?? extraction.StatementCardTypeId, resolvedCard: resolvedCard));
        }

        foreach (var receipt in receipts.Where(receipt => !usedReceiptIds.Contains(receipt.ReceiptId) && receipt.BankCardId > 0))
            response.UnknownMatches.Add(CreditReportResponseDto.FromExisting(receipt, null, LookupCard(receipt.BankCardId, bankCards)));

        foreach (var draft in drafts.Where(draft => !usedDraftIds.Contains(draft.ReceiptDraftId) && draft.BankCardId > 0))
            response.UnknownMatches.Add(CreditReportResponseDto.FromExisting(null, draft, LookupCard(draft.BankCardId, bankCards)));

        _logger.LogError("[CreditReportTrace] Step=Complete Complete={Complete} DraftMatches={DraftMatches} Proposed={Proposed} Unknown={Unknown}", response.CompleteMatches.Count, response.DraftMatches.Count, response.CreatedDrafts.Count, response.UnknownMatches.Count);
        return response;
    }

    public async Task<CreditReportResponseDto> CreateDraftsAsync(CreditReportCreateDraftsRequestDto dto, Guid currentUser, CancellationToken cancellationToken = default)
    {
        var response = new CreditReportResponseDto();
        var bankCards = await LoadBankCardsAsync(dto.OrganizationId, dto.OfficeId);
        foreach (var lineDto in dto.Lines ?? [])
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = new CreditCardStatementLine
            {
                ChargeDate = lineDto.ChargeDate,
                Amount = lineDto.Amount,
                VendorName = lineDto.VendorName,
                Description = lineDto.Description,
                CardLastFour = lineDto.CardLastFour,
                CardTypeId = lineDto.CardTypeId
            };
            var vendorId = lineDto.VendorId;
            var bankCard = lineDto.BankCardId is > 0
                ? bankCards.FirstOrDefault(card => card.BankCardId == lineDto.BankCardId.Value)
                : ResolveBankCard(lineDto.CardLastFour, lineDto.CardTypeId, bankCards);
            var created = await CreateStatementDraftAsync(dto.OrganizationId, dto.OfficeId, line, vendorId, bankCard, currentUser);
            response.CreatedDrafts.Add(CreditReportResponseDto.FromLine(line, draft: created, vendorId: created.VendorId, bankCardId: created.BankCardId, cardTypeId: line.CardTypeId, resolvedCard: bankCard));
        }

        _logger.LogError("[CreditReportTrace] Step=CreateDrafts Created={Created}", response.CreatedDrafts.Count);
        return response;
    }

    private async Task<List<BankCard>> LoadBankCardsAsync(Guid organizationId, int? officeId)
    {
        var offices = (await _organizationRepository.GetOfficesByOrganizationIdAsync(organizationId)).ToList();
        var officeIds = officeId is > 0 ? officeId.Value.ToString() : string.Join(',', offices.Select(office => office.OfficeId).Where(id => id > 0));
        if (string.IsNullOrWhiteSpace(officeIds))
            return [];

        return await _accountingRepository.GetBankCardsByOfficeIdsAsync(organizationId, officeIds);
    }

    private async Task<ReceiptDraft> EnrichMatchedDraftAsync(ReceiptDraft draft, CreditCardStatementLine line, Contact? vendor, BankCard? card, int? requestOfficeId, Guid currentUser)
    {
        if (!TryEnrichDraftFromStatement(draft, line, vendor, card, requestOfficeId))
            return draft;

        draft.ModifiedBy = currentUser;
        var updated = await _maintenanceRepository.UpdateReceiptDraftAsync(draft);
        _logger.LogError("[CreditReportTrace] Step=EnrichDraft DraftId={DraftId} DraftCode={DraftCode} VendorId={VendorId} BankCardId={BankCardId}", updated.ReceiptDraftId, updated.DraftCode, updated.VendorId, updated.BankCardId);
        return updated;
    }

    private static bool TryEnrichDraftFromStatement(ReceiptDraft draft, CreditCardStatementLine line, Contact? vendor, BankCard? card, int? requestOfficeId)
    {
        var changed = false;

        if ((draft.VendorId is null || draft.VendorId == Guid.Empty) && vendor?.ContactId is { } vendorId && vendorId != Guid.Empty)
        {
            draft.VendorId = vendorId;
            changed = true;
        }

        var cleanedVendor = CreditCardStatementLineParser.CleanVendorName(draft.VendorName)
            ?? CreditCardStatementLineParser.CleanVendorName(line.VendorName);
        if (!string.IsNullOrWhiteSpace(cleanedVendor)
            && !cleanedVendor.Equals((draft.VendorName ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
        {
            draft.VendorName = cleanedVendor;
            changed = true;
        }

        if (draft.BankCardId is not > 0 && card?.BankCardId > 0)
        {
            draft.BankCardId = card.BankCardId;
            draft.BankCardDisplayName = card.DisplayName;
            if (draft.PaidAmount == 0 && draft.Amount != 0)
            {
                draft.PaidAmount = draft.Amount;
                draft.PaidDate = draft.ReceiptDate ?? line.ChargeDate;
                draft.PaymentTypeId = (int)PaymentType.CreditCard;
            }

            if (draft.OfficeId is not > 0 && card.OfficeId > 0)
                draft.OfficeId = card.OfficeId;

            changed = true;
        }

        if (draft.OfficeId is not > 0 && requestOfficeId is > 0)
        {
            draft.OfficeId = requestOfficeId;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(draft.ExtractionJson))
        {
            draft.ExtractionJson = JsonSerializer.Serialize(line);
            changed = true;
        }
        else if (changed)
        {
            draft.ExtractionJson = JsonSerializer.Serialize(line);
        }

        if (!draft.DueDate.HasValue && (draft.ReceiptDate ?? line.ChargeDate) is { } dueDate)
        {
            draft.DueDate = dueDate;
            changed = true;
        }

        if (!draft.AccountingPeriod.HasValue && (draft.ReceiptDate ?? line.ChargeDate) is { } periodDate)
        {
            draft.AccountingPeriod = new DateOnly(periodDate.Year, periodDate.Month, 1);
            changed = true;
        }

        return changed;
    }

    private async Task<ReceiptDraft> CreateStatementDraftAsync(Guid organizationId, int? requestOfficeId, CreditCardStatementLine line, Guid? vendorId, BankCard? bankCard, Guid currentUser)
    {
        var draftCode = await _organizationManager.GenerateEntityCodeAsync(organizationId, EntityType.ReceiptDraft);
        if (string.IsNullOrWhiteSpace(draftCode))
            throw new InvalidOperationException("Unable to generate receipt draft code");

        var chargeDate = line.ChargeDate;
        var amount = line.Amount ?? 0;
        var officeId = requestOfficeId is > 0 ? requestOfficeId : bankCard?.OfficeId;
        var draft = new ReceiptDraft
        {
            OrganizationId = organizationId,
            OfficeId = officeId is > 0 ? officeId : null,
            DraftCode = draftCode.Trim(),
            PropertyIds = [ReceiptPropertyConstants.CompanyPropertyId],
            ReceiptDate = chargeDate,
            DueDate = chargeDate,
            AccountingPeriod = chargeDate.HasValue ? new DateOnly(chargeDate.Value.Year, chargeDate.Value.Month, 1) : null,
            Amount = amount,
            Description = null,
            BankCardId = bankCard?.BankCardId,
            VendorId = vendorId,
            VendorName = CreditCardStatementLineParser.CleanVendorName(line.VendorName) ?? line.VendorName,
            PaidAmount = bankCard?.BankCardId > 0 ? amount : 0,
            PaidDate = bankCard?.BankCardId > 0 ? chargeDate : null,
            PaymentTypeId = bankCard?.BankCardId > 0 ? (int)PaymentType.CreditCard : 0,
            DraftSourceFlags = ReceiptDraftSourceFlags.StatementImport,
            ExtractionJson = JsonSerializer.Serialize(line),
            IsActive = true,
            CreatedBy = currentUser,
            ModifiedBy = currentUser
        };

        return await _maintenanceRepository.CreateReceiptDraftAsync(draft);
    }

    private static bool IsExactMatch(CreditCardStatementLine line, DateOnly existingDate, decimal existingAmount, Guid? existingVendorId, string? existingVendorName, int? existingBankCardId, string? existingCardLastFour, Guid? statementVendorId, int? statementBankCardId, string? statementLastFour)
    {
        if (!line.ChargeDate.HasValue || line.ChargeDate.Value != existingDate)
            return false;

        if (!line.Amount.HasValue || Math.Abs(decimal.Round(line.Amount.Value, 2) - decimal.Round(existingAmount, 2)) > 0.005m)
            return false;

        if (!VendorsMatch(statementVendorId, line.VendorName, existingVendorId, existingVendorName))
            return false;

        return CardsMatch(statementBankCardId, statementLastFour, existingBankCardId, existingCardLastFour);
    }

    private static bool VendorsMatch(Guid? statementVendorId, string? statementVendorName, Guid? existingVendorId, string? existingVendorName)
    {
        if (statementVendorId.HasValue && existingVendorId.HasValue && statementVendorId.Value != Guid.Empty && existingVendorId.Value != Guid.Empty)
            return statementVendorId.Value == existingVendorId.Value;

        var left = NormalizeVendorName(statementVendorName);
        var right = NormalizeVendorName(existingVendorName);
        if (VendorNamesAlign(left, right))
            return true;

        return string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right);
    }

    private static bool VendorNamesAlign(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        if (left == right)
            return true;

        return (left.Length >= 6 && right.Contains(left)) || (right.Length >= 6 && left.Contains(right));
    }

    private static bool CardsMatch(int? statementBankCardId, string? statementLastFour, int? existingBankCardId, string? existingLastFour)
    {
        if (statementBankCardId is > 0 && existingBankCardId is > 0)
            return statementBankCardId.Value == existingBankCardId.Value;

        if (LastDigitsMatch(statementLastFour, existingLastFour))
            return true;

        var statementHasCard = statementBankCardId is > 0 || HasCardDigits(statementLastFour);
        var existingHasCard = existingBankCardId is > 0 || HasCardDigits(existingLastFour);
        return !statementHasCard || !existingHasCard;
    }

    private static Contact? ResolveVendor(string? vendorName, IReadOnlyList<Contact> vendors)
    {
        var normalized = NormalizeVendorName(vendorName);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        var matches = vendors.Where(vendor => GetVendorNames(vendor).Contains(normalized)).ToList();
        return matches.Count == 1 ? matches[0] : null;
    }

    private static BankCard? ResolveBankCard(string? cardLastFour, int? cardTypeId, IReadOnlyList<BankCard> bankCards)
    {
        if (!HasCardDigits(cardLastFour))
            return bankCards.Count == 1 ? bankCards[0] : null;

        var matches = bankCards.Where(card => LastDigitsMatch(cardLastFour, card.LastFour)).ToList();
        if (matches.Count > 1 && cardTypeId.HasValue)
            matches = matches.Where(card => card.CardTypeId == cardTypeId.Value).ToList();

        return matches.Count == 1 ? matches[0] : null;
    }

    private static BankCard? LookupCard(int? bankCardId, IReadOnlyList<BankCard> bankCards)
    {
        if (bankCardId is not > 0)
            return null;

        return bankCards.FirstOrDefault(card => card.BankCardId == bankCardId.Value);
    }

    private static string? LookupLastFour(int? bankCardId, IReadOnlyList<BankCard> bankCards)
    {
        if (bankCardId is not > 0)
            return null;

        return bankCards.FirstOrDefault(card => card.BankCardId == bankCardId.Value)?.LastFour;
    }

    private static (DateOnly? StartDate, DateOnly? EndDate) ResolveDateRange(IReadOnlyList<CreditCardStatementLine> lines)
    {
        var dates = lines.Select(line => line.ChargeDate).Where(date => date.HasValue).Select(date => date!.Value).ToList();
        if (dates.Count == 0)
            return (null, null);

        return (dates.Min(), dates.Max());
    }

    private static HashSet<string> GetVendorNames(Contact vendor)
    {
        return new[] { vendor.DisplayName, vendor.CompanyName, vendor.FullName, vendor.LegalName, vendor.PreferredName }
            .Select(NormalizeVendorName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.Ordinal)!;
    }

    private static string NormalizeVendorName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var cleaned = Regex.Replace(value.Trim().ToUpperInvariant(), @"[^A-Z0-9]+", string.Empty);
        return cleaned;
    }

    private static string NormalizeCardDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length >= 4)
            return digits[^4..];

        return digits.Length >= 3 ? digits : string.Empty;
    }

    private static bool HasCardDigits(string? value) => !string.IsNullOrWhiteSpace(NormalizeCardDigits(value));

    private static bool LastDigitsMatch(string? left, string? right)
    {
        var a = NormalizeCardDigits(left);
        var b = NormalizeCardDigits(right);
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return false;

        var length = Math.Min(a.Length, b.Length);
        return a[^length..] == b[^length..];
    }
}
