using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    private readonly record struct JournalEntryLineContext(
        Guid? PropertyId = null,
        string? PropertyCode = null,
        Guid? ReservationId = null,
        string? ReservationCode = null,
        Guid? ContactId = null,
        string? ContactName = null);

    private static void ApplyJournalEntryLineContext(JournalEntryLine line, JournalEntryLineContext context)
    {
        if (context.PropertyId is { } propertyId && propertyId != Guid.Empty)
            line.PropertyId = propertyId;

        var propertyCode = NormalizeOptionalString(context.PropertyCode);
        if (propertyCode != null)
            line.PropertyCode = propertyCode;

        if (context.ReservationId is { } reservationId && reservationId != Guid.Empty)
            line.ReservationId = reservationId;

        var reservationCode = NormalizeOptionalString(context.ReservationCode);
        if (reservationCode != null)
            line.ReservationCode = reservationCode;

        if (context.ContactId is { } contactId && contactId != Guid.Empty)
            line.ContactId = contactId;

        var contactName = NormalizeOptionalString(context.ContactName);
        if (contactName != null)
            line.ContactName = contactName;
    }

    private static JournalEntryLineContext CreateJournalEntryLineContextFromInvoice(Invoice invoice, Guid? propertyId = null)
        => new(
            NormalizeOptionalGuid(propertyId ?? invoice.PropertyId),
            NormalizeOptionalString(invoice.PropertyCode),
            NormalizeOptionalGuid(invoice.ReservationId),
            NormalizeOptionalString(invoice.ReservationCode),
            NormalizeOptionalGuid(invoice.ContactId),
            NormalizeOptionalString(invoice.ContactName));

    private static JournalEntryLineContext CreateJournalEntryLineContextFromInvoicePayment(Invoice invoice, LedgerLine? paymentLedgerLine, Guid? propertyId = null)
    {
        var reservationId = NormalizeOptionalGuid(paymentLedgerLine?.ReservationId) ?? NormalizeOptionalGuid(invoice.ReservationId);
        return new JournalEntryLineContext(
            NormalizeOptionalGuid(propertyId ?? invoice.PropertyId),
            NormalizeOptionalString(invoice.PropertyCode),
            reservationId,
            NormalizeOptionalString(invoice.ReservationCode),
            NormalizeOptionalGuid(invoice.ContactId),
            NormalizeOptionalString(invoice.ContactName));
    }

    private static JournalEntryLineContext CreateJournalEntryLineContextFromReceipt(Receipt receipt, Guid? propertyId = null, Guid? contactId = null, string? contactName = null)
        => new(
            NormalizeOptionalGuid(propertyId) ?? FirstReceiptPropertyId(receipt),
            null,
            null,
            null,
            NormalizeOptionalGuid(contactId ?? receipt.VendorId),
            NormalizeOptionalString(contactName ?? receipt.VendorName));

    private static JournalEntryLineContext CreateJournalEntryLineContextFromReceiptSplit(Receipt receipt, ReceiptSplit split, Guid? contactId = null, string? contactName = null)
        => new(
            NormalizeOptionalGuid(split.PropertyId) ?? FirstReceiptPropertyId(receipt),
            null,
            null,
            null,
            NormalizeOptionalGuid(contactId ?? receipt.VendorId),
            NormalizeOptionalString(contactName ?? receipt.VendorName));

    private static JournalEntryLineContext CreateJournalEntryLineContextFromWorkOrder(WorkOrder workOrder, Guid? contactId, string? contactName = null)
        => new(
            NormalizeOptionalGuid(workOrder.PropertyId),
            NormalizeOptionalString(workOrder.PropertyCode),
            NormalizeOptionalGuid(workOrder.ReservationId),
            NormalizeOptionalString(workOrder.ReservationCode),
            NormalizeOptionalGuid(contactId),
            NormalizeOptionalString(contactName));

    private static JournalEntryLineContext CreateJournalEntryLineContextFromDepositSplit(DepositSplit split)
        => new(
            NormalizeOptionalGuid(split.PropertyId),
            NormalizeOptionalString(split.PropertyCode),
            NormalizeOptionalGuid(split.ReservationId),
            NormalizeOptionalString(split.ReservationCode),
            NormalizeOptionalGuid(split.ContactId),
            NormalizeOptionalString(split.ContactName));

    private static JournalEntryLineContext CreateJournalEntryLineContextFromTransferSplit(TransferSplit split)
        => new(
            NormalizeOptionalGuid(split.PropertyId),
            NormalizeOptionalString(split.PropertyCode),
            NormalizeOptionalGuid(split.ReservationId),
            NormalizeOptionalString(split.ReservationCode),
            NormalizeOptionalGuid(split.ContactId),
            NormalizeOptionalString(split.ContactName));

    private static JournalEntryLineContext FirstDepositSplitContext(IEnumerable<DepositSplit> splits)
    {
        var propertyId = FirstSplitContextId(splits, split => split.PropertyId);
        var reservationId = FirstSplitContextId(splits, split => split.ReservationId);
        var contactId = FirstSplitContextId(splits, split => split.ContactId);
        var propertyCode = splits.Select(split => NormalizeOptionalString(split.PropertyCode)).FirstOrDefault(code => code != null);
        var reservationCode = splits.Select(split => NormalizeOptionalString(split.ReservationCode)).FirstOrDefault(code => code != null);
        var contactName = splits.Select(split => NormalizeOptionalString(split.ContactName)).FirstOrDefault(name => name != null);

        return new JournalEntryLineContext(propertyId, propertyCode, reservationId, reservationCode, contactId, contactName);
    }

    private static JournalEntryLineContext FirstTransferSplitContext(IEnumerable<TransferSplit> splits)
    {
        var propertyId = FirstSplitContextId(splits, split => split.PropertyId);
        var reservationId = FirstSplitContextId(splits, split => split.ReservationId);
        var contactId = FirstSplitContextId(splits, split => split.ContactId);
        var propertyCode = splits.Select(split => NormalizeOptionalString(split.PropertyCode)).FirstOrDefault(code => code != null);
        var reservationCode = splits.Select(split => NormalizeOptionalString(split.ReservationCode)).FirstOrDefault(code => code != null);
        var contactName = splits.Select(split => NormalizeOptionalString(split.ContactName)).FirstOrDefault(name => name != null);

        return new JournalEntryLineContext(propertyId, propertyCode, reservationId, reservationCode, contactId, contactName);
    }

    private static Guid? FirstReceiptPropertyId(Receipt receipt)
        => receipt.PropertyIds.FirstOrDefault(id => id != Guid.Empty) is { } id && id != Guid.Empty ? id : null;

    private static string? ResolveJournalEntrySourceCodeFromInvoice(Invoice invoice)
        => NormalizeOptionalString(invoice.InvoiceCode);

    private static string? ResolveJournalEntrySourceCodeFromReceipt(Receipt receipt)
        => NormalizeOptionalString(receipt.BillNumber) ?? NormalizeOptionalString(receipt.ReceiptCode);

    private static string? ResolveJournalEntrySourceCodeFromDeposit(Deposit deposit)
        => NormalizeOptionalString(deposit.DepositCode);

    private static string? ResolveJournalEntrySourceCodeFromTransfer(Transfer transfer)
        => NormalizeOptionalString(transfer.TransferCode);

    private static string? ResolveJournalEntrySourceCodeFromWorkOrder(WorkOrder workOrder)
        => NormalizeOptionalString(workOrder.WorkOrderCode);

    private static string? ResolveJournalEntrySourceCodeFromReservation(ReservationDeparture reservation)
        => NormalizeOptionalString(reservation.ReservationCode);

    private static Guid? ResolveReservationContactId(ReservationDeparture reservation, Reservation? reservationDetail = null)
    {
        var detailContactId = reservationDetail?.ContactIds.FirstOrDefault(id => id != Guid.Empty);
        if (detailContactId is { } resolvedDetailContactId && resolvedDetailContactId != Guid.Empty)
            return resolvedDetailContactId;

        if (reservation.ContactId != Guid.Empty)
            return reservation.ContactId;

        var companyId = reservation.CompanyId ?? reservationDetail?.CompanyId;
        if (companyId is { } companyContactId && companyContactId != Guid.Empty)
            return companyContactId;

        return null;
    }

    private static JournalEntryLineContext CreateJournalEntryLineContextFromReservation(ReservationDeparture reservation, Reservation? reservationDetail = null)
    {
        var contactId = ResolveReservationContactId(reservation, reservationDetail);
        var contactName = NormalizeOptionalString(reservationDetail?.ContactName)
            ?? NormalizeOptionalString(reservation.ContactName)
            ?? NormalizeOptionalString(reservation.CompanyName)
            ?? NormalizeOptionalString(reservationDetail?.CompanyName)
            ?? NormalizeOptionalString(reservation.TenantName)
            ?? NormalizeOptionalString(reservationDetail?.TenantName);

        return new JournalEntryLineContext(
            NormalizeOptionalGuid(reservation.PropertyId),
            NormalizeOptionalString(reservation.PropertyCode),
            NormalizeOptionalGuid(reservation.ReservationId),
            NormalizeOptionalString(reservation.ReservationCode),
            NormalizeOptionalGuid(contactId),
            contactName);
    }

    private async Task<JournalEntryLineContext> ResolveReservationJournalEntryLineContextAsync(
        Guid organizationId,
        ReservationDeparture reservation,
        Reservation reservationDetail)
    {
        var context = CreateJournalEntryLineContextFromReservation(reservation, reservationDetail);
        if (context.ContactId is not { } contactId)
            return context;

        var contactName = context.ContactName;
        if (contactName == null)
        {
            var contact = await _contactRepository.GetContactByIdAsync(contactId, organizationId);
            contactName = NormalizeOptionalString(contact?.DisplayName)
                ?? NormalizeOptionalString(contact?.CompanyName)
                ?? NormalizeOptionalString(contact?.FullName);
        }

        return context with { ContactName = contactName };
    }

    private static string? NormalizeOptionalString(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static Guid? ResolveInvoiceResponsibleContactId(Reservation reservation)
    {
        if (reservation.ReservationType is ReservationType.Corporate or ReservationType.Platform)
            return NormalizeOptionalGuid(reservation.CompanyId);

        var primaryContactId = reservation.ContactIds.FirstOrDefault(id => id != Guid.Empty);
        return primaryContactId != Guid.Empty ? primaryContactId : null;
    }

    private static string? ResolveInvoiceResponsibleContactName(Contact contact, ReservationType reservationType)
    {
        if (reservationType is ReservationType.Corporate or ReservationType.Platform)
            return NormalizeOptionalString(contact.DisplayName ?? contact.CompanyName ?? contact.FullName);

        if (contact.EntityType == EntityType.Company)
            return NormalizeOptionalString(contact.CompanyName ?? contact.DisplayName ?? contact.FullName);

        var personName = $"{contact.FirstName ?? string.Empty} {contact.LastName ?? string.Empty}".Trim();
        return NormalizeOptionalString(personName) ?? NormalizeOptionalString(contact.FullName);
    }

    private async Task<JournalEntryLineContext> ResolveInvoiceJournalEntryLineContextAsync(Invoice invoice, LedgerLine? paymentLedgerLine = null)
    {
        var propertyId = NormalizeOptionalGuid(invoice.PropertyId);
        if (!propertyId.HasValue)
            propertyId = await ResolveInvoicePropertyIdAsync(invoice);

        var propertyCode = NormalizeOptionalString(invoice.PropertyCode);
        if (propertyId.HasValue && propertyCode == null)
        {
            var property = await _propertyRepository.GetPropertyByIdAsync(propertyId.Value, invoice.OrganizationId);
            propertyCode = NormalizeOptionalString(property?.PropertyCode);
        }

        var contactId = NormalizeOptionalGuid(invoice.ContactId);
        var contactName = NormalizeOptionalString(invoice.ContactName) ?? NormalizeOptionalString(invoice.ResponsibleParty);
        Reservation? reservation = null;

        if ((!contactId.HasValue || contactName == null)
            && invoice.ReservationId is { } reservationId
            && reservationId != Guid.Empty
            && reservationId != SystemOrganization)
        {
            reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, invoice.OrganizationId);
            if (reservation != null && !contactId.HasValue)
                contactId = ResolveInvoiceResponsibleContactId(reservation);
        }

        if (contactId is { } resolvedContactIdValue && resolvedContactIdValue != Guid.Empty && contactName == null)
        {
            var contact = await _contactRepository.GetContactByIdAsync(resolvedContactIdValue, invoice.OrganizationId);
            contactName = contact == null
                ? null
                : reservation != null
                    ? ResolveInvoiceResponsibleContactName(contact, reservation.ReservationType)
                    : NormalizeOptionalString(contact.DisplayName ?? contact.CompanyName ?? contact.FullName);
        }

        var lineReservationId = NormalizeOptionalGuid(paymentLedgerLine?.ReservationId) ?? NormalizeOptionalGuid(invoice.ReservationId);
        var reservationCode = NormalizeOptionalString(invoice.ReservationCode);
        if (lineReservationId.HasValue && reservationCode == null)
        {
            reservation ??= await _reservationRepository.GetReservationByIdAsync(lineReservationId.Value, invoice.OrganizationId);
            reservationCode = NormalizeOptionalString(reservation?.ReservationCode);
        }

        return new JournalEntryLineContext(propertyId, propertyCode, lineReservationId, reservationCode, contactId, contactName);
    }

    private async Task<JournalEntryLineContext> ResolveReceiptJournalEntryLineContextAsync(Receipt receipt, Guid? propertyId = null, Guid? contactId = null, string? contactName = null)
    {
        var resolvedPropertyId = NormalizeOptionalGuid(propertyId) ?? FirstReceiptPropertyId(receipt);
        string? propertyCode = null;

        if (resolvedPropertyId.HasValue)
        {
            var property = await _propertyRepository.GetPropertyByIdAsync(resolvedPropertyId.Value, receipt.OrganizationId);
            propertyCode = NormalizeOptionalString(property?.PropertyCode);
        }

        var resolvedContactId = NormalizeOptionalGuid(contactId ?? receipt.VendorId);
        var resolvedContactName = NormalizeOptionalString(contactName ?? receipt.VendorName);
        if (resolvedContactId.HasValue && resolvedContactName == null)
        {
            var contact = await _contactRepository.GetContactByIdAsync(resolvedContactId.Value, receipt.OrganizationId);
            resolvedContactName = NormalizeOptionalString(contact?.DisplayName ?? contact?.CompanyName ?? contact?.FullName);
        }

        return new JournalEntryLineContext(
            resolvedPropertyId,
            propertyCode,
            null,
            null,
            resolvedContactId,
            resolvedContactName);
    }
}
