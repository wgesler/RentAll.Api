using RentAll.Api.Dtos.Maintenances.Receipts;
using RentAll.Domain.Constants;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Models;
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
    private readonly IAccountingManager _accountingManager;
    private readonly ILogger<CreditReportService> _logger;
    private const int MatchDateWindowDays = 5;

    public CreditReportService(IDocumentIntelligenceService documentIntelligenceService, IMaintenanceRepository maintenanceRepository, IOrganizationRepository organizationRepository, IOrganizationManager organizationManager, IAccountingRepository accountingRepository, IContactRepository contactRepository, IAccountingManager accountingManager, ILogger<CreditReportService> logger)
    {
        _documentIntelligenceService = documentIntelligenceService;
        _maintenanceRepository = maintenanceRepository;
        _organizationRepository = organizationRepository;
        _organizationManager = organizationManager;
        _accountingRepository = accountingRepository;
        _contactRepository = contactRepository;
        _accountingManager = accountingManager;
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
            IsActive = null,
            IncludeInactive = true,
            StartDate = dateRange.StartDate,
            EndDate = dateRange.EndDate
        })).ToList();
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

        var statementCardTypeId = ResolveClearStatementCardType(extraction, fileName, bankCards);
        if (statementCardTypeId.HasValue)
        {
            var skippedReceipts = receipts.RemoveAll(receipt => IsAssociatedWithOtherCard(receipt.BankCardId, bankCards, statementCardTypeId.Value));
            var skippedDrafts = drafts.RemoveAll(draft => IsAssociatedWithOtherCard(draft.BankCardId, bankCards, statementCardTypeId.Value));
            _logger.LogError("[CreditReportTrace] Step=CardTypeFilter CardType={CardType} SkippedReceipts={SkippedReceipts} SkippedDrafts={SkippedDrafts}", (CardType)statementCardTypeId.Value, skippedReceipts, skippedDrafts);
        }

        var aliases = (await _maintenanceRepository.GetReceiptMatchesByOrganizationIdAsync(dto.OrganizationId)).ToList();
        var billPayments = await LoadCardBillPaymentsAsync(dto.OrganizationId, officeIds, dateRange.StartDate, dateRange.EndDate, bankCards, statementCardTypeId);
        _logger.LogError("[CreditReportTrace] Step=Extracted LineCount={LineCount} OfficeId={OfficeId} AliasCount={AliasCount} BillPayments={BillPayments}", extraction.Lines.Count, dto.OfficeId, aliases.Count, billPayments.Count);
        var receiptMatches = AssignClosestMatches(extraction.Lines, receipts, receipt => receipt.ReceiptId, (line, receipt) =>
        {
            var statementVendor = ResolveStatementVendor(line.VendorName, vendors, aliases);
            return IsExactMatch(line, receipt.ReceiptDate, receipt.Amount, receipt.VendorId, receipt.VendorName, statementVendor.VendorId, statementVendor.VendorName, line.VendorName);
        }, (line, receipt) => DateDistance(line.ChargeDate, receipt.ReceiptDate));
        var unmatchedAfterReceipts = extraction.Lines.Where(line => !receiptMatches.ContainsKey(line)).ToList();
        var billPaymentMatches = AssignClosestMatches(unmatchedAfterReceipts, billPayments, payment => payment.PaymentId, (line, payment) =>
        {
            var statementVendor = ResolveStatementVendor(line.VendorName, vendors, aliases);
            return BillPaymentMatches(line, payment, statementVendor.VendorId, statementVendor.VendorName);
        }, (line, payment) => DateDistance(line.ChargeDate, payment.PaymentDate));
        var unmatchedLines = unmatchedAfterReceipts.Where(line => !billPaymentMatches.ContainsKey(line)).ToList();
        var draftMatches = AssignClosestMatches(unmatchedLines, drafts.Where(draft => draft.ReceiptDate.HasValue).ToList(), draft => draft.ReceiptDraftId, (line, draft) =>
        {
            var statementVendor = ResolveStatementVendor(line.VendorName, vendors, aliases);
            return IsExactMatch(line, draft.ReceiptDate!.Value, draft.Amount, draft.VendorId, draft.VendorName, statementVendor.VendorId, statementVendor.VendorName, line.VendorName);
        }, (line, draft) => DateDistance(line.ChargeDate, draft.ReceiptDate!.Value));
        var usedReceiptIds = receiptMatches.Values.Select(receipt => receipt.ReceiptId).ToHashSet();
        var usedDraftIds = draftMatches.Values.Select(draft => draft.ReceiptDraftId).ToHashSet();
        foreach (var line in extraction.Lines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var statementVendor = ResolveStatementVendor(line.VendorName, vendors, aliases);
            var resolvedVendor = statementVendor.VendorId is Guid vendorId ? vendors.FirstOrDefault(vendor => vendor.ContactId == vendorId) : ResolveVendor(statementVendor.VendorName, vendors);
            var statementLastFour = line.CardLastFour ?? extraction.StatementCardLastFour;
            var resolvedCard = ResolveBankCard(statementLastFour, line.CardTypeId ?? extraction.StatementCardTypeId, bankCards);
            if (receiptMatches.TryGetValue(line, out var matchedReceipt))
            {
                matchedReceipt = await CorrectReceiptCardAsync(matchedReceipt, resolvedCard, currentUser);
                response.CompleteMatches.Add(CreditReportResponseDto.FromLine(line, matchedReceipt, resolvedCard: resolvedCard));
                continue;
            }

            if (billPaymentMatches.TryGetValue(line, out var matchedPayment))
            {
                response.CompleteMatches.Add(CreditReportResponseDto.FromBillPayment(line, matchedPayment, resolvedCard));
                continue;
            }

            if (draftMatches.TryGetValue(line, out var matchedDraft))
            {
                matchedDraft = await EnrichMatchedDraftAsync(matchedDraft, line, resolvedVendor, resolvedCard, dto.OfficeId, currentUser);
                response.DraftMatches.Add(CreditReportResponseDto.FromLine(line, draft: matchedDraft, resolvedCard: resolvedCard));
                continue;
            }

            response.CreatedDrafts.Add(CreditReportResponseDto.FromLine(line, vendorId: statementVendor.VendorId ?? resolvedVendor?.ContactId, bankCardId: resolvedCard?.BankCardId, cardTypeId: line.CardTypeId ?? extraction.StatementCardTypeId, resolvedCard: resolvedCard));
        }

        foreach (var receipt in receipts.Where(receipt => !usedReceiptIds.Contains(receipt.ReceiptId) && receipt.BankCardId > 0))
            response.UnknownMatches.Add(CreditReportResponseDto.FromExisting(receipt, null, LookupCard(receipt.BankCardId, bankCards)));

        foreach (var draft in drafts.Where(draft => !usedDraftIds.Contains(draft.ReceiptDraftId) && draft.BankCardId > 0))
            response.UnknownMatches.Add(CreditReportResponseDto.FromExisting(null, draft, LookupCard(draft.BankCardId, bankCards)));

        _logger.LogError("[CreditReportTrace] Step=Complete Complete={Complete} BillPayments={BillPayments} DraftMatches={DraftMatches} Proposed={Proposed} Unknown={Unknown}", response.CompleteMatches.Count, billPaymentMatches.Count, response.DraftMatches.Count, response.CreatedDrafts.Count, response.UnknownMatches.Count);
        return response;
    }

    public async Task SaveMatchesAsync(CreditReportSaveMatchesRequestDto dto)
    {
        var existing = (await _maintenanceRepository.GetReceiptMatchesByOrganizationIdAsync(dto.OrganizationId)).ToList();
        foreach (var match in dto.Matches ?? [])
        {
            var sourceName = (match.SourceName ?? string.Empty).Trim();
            var matchedName = string.IsNullOrWhiteSpace(match.MatchedName) ? null : match.MatchedName.Trim();
            Guid? matchedId = match.MatchedId is Guid id && id != Guid.Empty ? id : null;
            if (string.IsNullOrWhiteSpace(sourceName) || (matchedId == null && string.IsNullOrWhiteSpace(matchedName)))
                continue;

            var found = existing.FirstOrDefault(item => SameSavedVendor(item.SourceName, sourceName));
            if (found == null)
            {
                var created = await _maintenanceRepository.CreateReceiptMatchAsync(new ReceiptMatch
                {
                    OrganizationId = dto.OrganizationId,
                    SourceName = sourceName,
                    MatchedId = matchedId,
                    MatchedName = matchedName,
                    IsActive = true
                });
                existing.Add(created);
                continue;
            }

            found.MatchedId = matchedId;
            found.MatchedName = matchedName;
            found.IsActive = true;
            await _maintenanceRepository.UpdateReceiptMatchAsync(found);
        }

        _logger.LogError("[CreditReportTrace] Step=SaveMatches Count={Count}", dto.Matches?.Count ?? 0);
    }

    public async Task OverwriteMatchedVendorNameAsync(Guid organizationId, string? previousName, string? vendorName, Guid? vendorId)
    {
        var previous = (previousName ?? string.Empty).Trim();
        var current = (vendorName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(previous) || string.IsNullOrWhiteSpace(current) || string.Equals(previous, current, StringComparison.OrdinalIgnoreCase))
            return;

        var aliases = (await _maintenanceRepository.GetReceiptMatchesByOrganizationIdAsync(organizationId)).ToList();
        foreach (var alias in aliases)
        {
            if (!string.Equals((alias.MatchedName ?? string.Empty).Trim(), previous, StringComparison.OrdinalIgnoreCase))
                continue;

            alias.MatchedName = current;
            if (vendorId is Guid id && id != Guid.Empty)
                alias.MatchedId = id;

            await _maintenanceRepository.UpdateReceiptMatchAsync(alias);
        }
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

    private async Task<Receipt> CorrectReceiptCardAsync(Receipt receipt, BankCard? card, Guid currentUser)
    {
        if (card?.BankCardId is not > 0 || receipt.BankCardId == card.BankCardId)
            return receipt;

        var current = await _maintenanceRepository.GetReceiptByIdAsync(receipt.ReceiptId, receipt.OrganizationId) ?? receipt;
        current.BankCardId = card.BankCardId;
        current.BankCardDisplayName = card.DisplayName;
        var updated = await _accountingManager.UpdateReceiptAsync(current, currentUser);
        if (string.IsNullOrWhiteSpace(updated.BankCardDisplayName))
            updated.BankCardDisplayName = card.DisplayName;

        _logger.LogError("[CreditReportTrace] Step=CorrectCard ReceiptId={ReceiptId} ReceiptCode={ReceiptCode} BankCardId={BankCardId}", updated.ReceiptId, updated.ReceiptCode, updated.BankCardId);
        return updated;
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

        if (card?.BankCardId > 0 && draft.BankCardId != card.BankCardId)
        {
            var fillingBlankCard = draft.BankCardId is not > 0;
            draft.BankCardId = card.BankCardId;
            draft.BankCardDisplayName = card.DisplayName;
            if (fillingBlankCard && draft.PaidAmount == 0 && draft.Amount != 0)
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

    private static int? ResolveClearStatementCardType(CreditCardStatementExtraction extraction, string? fileName, IReadOnlyList<BankCard> bankCards)
    {
        var header = extraction.FullText ?? string.Empty;
        if (header.Length > 3000)
            header = header[..3000];

        var identity = $"{fileName}\n{header}";
        var identityType = ResolveSingleCardType(identity);
        if (identityType.HasValue)
            return identityType;

        var fromCards = ResolveSingleTypeFromStatementCards(extraction, bankCards);
        if (fromCards.HasValue || NamesMoreThanOneCardType(identity))
            return fromCards;

        return extraction.StatementCardTypeId is int statementCardTypeId && Enum.IsDefined(typeof(CardType), statementCardTypeId) ? statementCardTypeId : null;
    }

    private static bool NamesMoreThanOneCardType(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var count = 0;
        foreach (var cardType in new[] { CardType.Visa, CardType.MasterCard, CardType.Discover, CardType.AmericanExpress })
        {
            if (Regex.IsMatch(text, CardTypePattern(cardType), RegexOptions.IgnoreCase))
                count++;
        }

        return count > 1;
    }

    private static int? ResolveSingleCardType(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        int? found = null;
        foreach (var cardType in new[] { CardType.Visa, CardType.MasterCard, CardType.Discover, CardType.AmericanExpress })
        {
            if (!Regex.IsMatch(text, CardTypePattern(cardType), RegexOptions.IgnoreCase))
                continue;

            if (found.HasValue)
                return null;

            found = (int)cardType;
        }

        return found;
    }

    private static int? ResolveSingleTypeFromStatementCards(CreditCardStatementExtraction extraction, IReadOnlyList<BankCard> bankCards)
    {
        var lastFours = extraction.Lines.Select(line => line.CardLastFour).Append(extraction.StatementCardLastFour).Where(lastFour => HasCardDigits(lastFour)).Select(lastFour => lastFour!).Distinct(StringComparer.Ordinal).ToList();
        if (lastFours.Count == 0)
            return null;

        int? found = null;
        foreach (var lastFour in lastFours)
        {
            var matches = bankCards.Where(card => LastDigitsMatch(lastFour, card.LastFour)).Select(ResolveBankCardType).Where(cardType => cardType.HasValue).Select(cardType => cardType!.Value).Distinct().ToList();
            if (matches.Count == 0)
                continue;

            if (matches.Count > 1)
                return null;

            if (found.HasValue && found.Value != matches[0])
                return null;

            found = matches[0];
        }

        return found;
    }

    private static bool IsAssociatedWithOtherCard(int? bankCardId, IReadOnlyList<BankCard> bankCards, int statementCardTypeId)
    {
        if (bankCardId is not > 0)
            return false;

        var card = LookupCard(bankCardId, bankCards);
        return card == null || ResolveBankCardType(card) != statementCardTypeId;
    }

    private static int? ResolveBankCardType(BankCard card)
    {
        var name = $"{card.CardName} {card.DisplayName}";
        foreach (var cardType in new[] { CardType.AmericanExpress, CardType.MasterCard, CardType.Discover, CardType.Visa })
        {
            if (Regex.IsMatch(name, CardTypePattern(cardType), RegexOptions.IgnoreCase))
                return (int)cardType;
        }

        return Enum.IsDefined(typeof(CardType), card.CardTypeId) ? card.CardTypeId : null;
    }

    private static string CardTypePattern(CardType cardType)
    {
        return cardType switch
        {
            CardType.Visa => @"\bvisa\b",
            CardType.MasterCard => @"\b(?:master\s*card|mastercard|\bmc\b)\b",
            CardType.Discover => @"\b(?:discover|\bdisc\b)\b",
            _ => @"\b(?:amex|american\s*express)\b"
        };
    }

    private async Task<List<Payment>> LoadCardBillPaymentsAsync(Guid organizationId, string officeIds, DateOnly? startDate, DateOnly? endDate, IReadOnlyList<BankCard> bankCards, int? statementCardTypeId)
    {
        if (string.IsNullOrWhiteSpace(officeIds))
            return [];

        var payments = await _accountingRepository.GetPaymentsByOfficeIdsAsync(organizationId, officeIds, (int)PaymentKind.Bill);
        return payments.Where(payment => payment.IsActive && payment.Amount != 0 && PaymentInDateRange(payment, startDate, endDate) && PaymentUsesStatementCard(payment, bankCards, statementCardTypeId)).ToList();
    }

    private static bool PaymentInDateRange(Payment payment, DateOnly? startDate, DateOnly? endDate)
    {
        if (payment.PaymentDate == default)
            return false;

        if (startDate.HasValue && payment.PaymentDate < startDate.Value)
            return false;

        return !endDate.HasValue || payment.PaymentDate <= endDate.Value;
    }

    private static bool PaymentUsesStatementCard(Payment payment, IReadOnlyList<BankCard> bankCards, int? statementCardTypeId)
    {
        var matchingCards = payment.ChartOfAccountId is > 0 ? bankCards.Where(card => card.ChartOfAccountId == payment.ChartOfAccountId).ToList() : [];
        if (matchingCards.Count > 0)
            return !statementCardTypeId.HasValue || matchingCards.Any(card => ResolveBankCardType(card) == statementCardTypeId.Value || !ResolveBankCardType(card).HasValue);

        return payment.PaymentTypeId == (int)PaymentType.CreditCard;
    }

    private static bool BillPaymentMatches(CreditCardStatementLine line, Payment payment, Guid? statementVendorId, string? statementVendorName)
    {
        if (!DatesWithinWindow(line.ChargeDate, payment.PaymentDate))
            return false;

        if (!line.Amount.HasValue || Math.Abs(decimal.Round(line.Amount.Value, 2) - decimal.Round(payment.Amount, 2)) > 0.005m)
            return false;

        return BillPaymentVendorMatches(payment, statementVendorId, statementVendorName, line.VendorName);
    }

    private static bool BillPaymentVendorMatches(Payment payment, Guid? statementVendorId, string? statementVendorName, string? rawStatementVendorName)
    {
        var allocations = payment.BillAllocations ?? [];
        var vendorIds = allocations.Select(allocation => allocation.VendorId).Where(id => id is Guid vendorId && vendorId != Guid.Empty).Select(id => id!.Value).Distinct().ToList();
        var vendorNames = allocations.Select(allocation => NormalizeVendorName(allocation.VendorName)).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.Ordinal).ToList();
        if (vendorIds.Count > 1 || vendorNames.Count > 1)
            return true;

        if (vendorIds.Count == 0 && vendorNames.Count == 0)
            return true;

        var vendorId = vendorIds.Count == 1 ? vendorIds[0] : (Guid?)null;
        var vendorName = allocations.Select(allocation => allocation.VendorName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(NormalizeVendorName(name)));
        return VendorsMatch(statementVendorId, statementVendorName, vendorId, vendorName, rawStatementVendorName);
    }

    private static bool IsExactMatch(CreditCardStatementLine line, DateOnly existingDate, decimal existingAmount, Guid? existingVendorId, string? existingVendorName, Guid? statementVendorId, string? statementVendorName = null, string? rawStatementVendorName = null)
    {
        if (!DatesWithinWindow(line.ChargeDate, existingDate))
            return false;

        if (!line.Amount.HasValue || Math.Abs(decimal.Round(line.Amount.Value, 2) - decimal.Round(existingAmount, 2)) > 0.005m)
            return false;

        if (!VendorsMatch(statementVendorId, statementVendorName ?? line.VendorName, existingVendorId, existingVendorName, rawStatementVendorName))
            return false;

        return true;
    }

    private static bool VendorsMatch(Guid? statementVendorId, string? statementVendorName, Guid? existingVendorId, string? existingVendorName, string? rawStatementVendorName = null)
    {
        var right = NormalizeVendorName(existingVendorName);
        if (VendorNamesAlign(NormalizeVendorName(statementVendorName), right) || VendorNamesAlign(NormalizeVendorName(rawStatementVendorName), right))
            return true;

        var statementId = statementVendorId is Guid leftId && leftId != Guid.Empty ? leftId : (Guid?)null;
        var existingId = existingVendorId is Guid rightId && rightId != Guid.Empty ? rightId : (Guid?)null;
        if (statementId.HasValue && existingId.HasValue)
            return statementId.Value == existingId.Value;

        var statementName = NormalizeVendorName(statementVendorName);
        var rawName = NormalizeVendorName(rawStatementVendorName);
        return string.IsNullOrWhiteSpace(right) || (string.IsNullOrWhiteSpace(statementName) && string.IsNullOrWhiteSpace(rawName));
    }

    private static bool VendorNamesAlign(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;

        if (left == right)
            return true;

        return (left.Length >= 6 && right.Contains(left)) || (right.Length >= 6 && left.Contains(right));
    }

    private static (Guid? VendorId, string? VendorName) ResolveStatementVendor(string? statementVendorName, IReadOnlyList<Contact> vendors, IReadOnlyList<ReceiptMatch> aliases)
    {
        var alias = FindReceiptMatch(statementVendorName, aliases);
        if (alias != null)
        {
            var vendor = alias.MatchedId is Guid matchedId && matchedId != Guid.Empty ? vendors.FirstOrDefault(item => item.ContactId == matchedId) : ResolveVendor(alias.MatchedName, vendors);
            var vendorId = vendor?.ContactId is Guid contactId && contactId != Guid.Empty ? contactId : (alias.MatchedId is Guid id && id != Guid.Empty ? id : (Guid?)null);
            var vendorName = string.IsNullOrWhiteSpace(alias.MatchedName) ? statementVendorName : alias.MatchedName;
            return (vendorId, vendorName);
        }

        var resolved = ResolveVendor(statementVendorName, vendors);
        return (resolved?.ContactId, statementVendorName);
    }

    private static ReceiptMatch? FindReceiptMatch(string? statementVendorName, IReadOnlyList<ReceiptMatch> aliases)
    {
        if (string.IsNullOrWhiteSpace(VendorMatchKey(statementVendorName)))
            return null;

        return aliases.FirstOrDefault(alias => SameSavedVendor(alias.SourceName, statementVendorName));
    }

    private static bool SameSavedVendor(string? savedSource, string? statementVendorName)
    {
        var saved = VendorMatchKey(savedSource);
        var statement = VendorMatchKey(statementVendorName);
        if (string.IsNullOrWhiteSpace(saved) || string.IsNullOrWhiteSpace(statement))
            return false;
        if (saved == statement)
            return true;

        return (saved.Length >= 6 && statement.Contains(saved)) || (statement.Length >= 6 && saved.Contains(statement));
    }

    private static string VendorMatchKey(string? value)
    {
        var cleaned = CreditCardStatementLineParser.CleanVendorName(value);
        return NormalizeVendorName(string.IsNullOrWhiteSpace(cleaned) ? value : cleaned);
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

        return (dates.Min().AddDays(-MatchDateWindowDays), dates.Max().AddDays(MatchDateWindowDays));
    }

    private static bool DatesWithinWindow(DateOnly? chargeDate, DateOnly existingDate)
    {
        return chargeDate.HasValue && Math.Abs(chargeDate.Value.DayNumber - existingDate.DayNumber) <= MatchDateWindowDays;
    }

    private static int DateDistance(DateOnly? chargeDate, DateOnly existingDate)
    {
        if (!chargeDate.HasValue)
            return int.MaxValue;

        return Math.Abs(chargeDate.Value.DayNumber - existingDate.DayNumber);
    }

    private static Dictionary<CreditCardStatementLine, T> AssignClosestMatches<T>(IReadOnlyList<CreditCardStatementLine> lines, IReadOnlyList<T> items, Func<T, Guid> itemId, Func<CreditCardStatementLine, T, bool> isMatch, Func<CreditCardStatementLine, T, int> dateDistance)
    {
        var pairs = new List<(int Distance, int LineIndex, T Item)>();
        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            foreach (var item in items)
            {
                if (!isMatch(lines[lineIndex], item))
                    continue;

                pairs.Add((dateDistance(lines[lineIndex], item), lineIndex, item));
            }
        }

        var assigned = new Dictionary<CreditCardStatementLine, T>();
        var usedIds = new HashSet<Guid>();
        foreach (var pair in pairs.OrderBy(pair => pair.Distance).ThenBy(pair => pair.LineIndex))
        {
            var id = itemId(pair.Item);
            if (assigned.ContainsKey(lines[pair.LineIndex]) || usedIds.Contains(id))
                continue;

            assigned[lines[pair.LineIndex]] = pair.Item;
            usedIds.Add(id);
        }

        return assigned;
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
