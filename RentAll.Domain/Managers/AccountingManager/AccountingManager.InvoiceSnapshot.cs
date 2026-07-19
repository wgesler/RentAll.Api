using RentAll.Domain.Enums;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    public async Task EnrichInvoiceBeforeSaveAsync(Invoice invoice)
    {
        if (invoice.ReservationId is { } reservationId && reservationId != Guid.Empty && reservationId != SystemOrganization)
        {
            var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId, invoice.OrganizationId);
            if (reservation != null)
            {
                invoice.PropertyId = NormalizeOptionalGuid(reservation.PropertyId);
                if (invoice.PropertyId.HasValue)
                {
                    var property = await _propertyRepository.GetPropertyByIdAsync(invoice.PropertyId.Value, invoice.OrganizationId);
                    invoice.PropertyCode = NormalizeOptionalString(property?.PropertyCode);
                }

                var resolvedContactId = NormalizeOptionalGuid(invoice.ContactId) ?? ResolveInvoiceResponsibleContactId(reservation);
                if (resolvedContactId.HasValue)
                {
                    invoice.ContactId = resolvedContactId;
                    var contact = await _contactRepository.GetContactByIdAsync(resolvedContactId.Value, invoice.OrganizationId);
                    if (contact != null)
                    {
                        invoice.ContactName = ResolveInvoiceResponsibleContactName(contact, reservation.ReservationType);
                        invoice.ResponsibleParty = invoice.ContactName;
                    }
                }

                invoice.ReservationCode = NormalizeOptionalString(invoice.ReservationCode) ?? NormalizeOptionalString(reservation.ReservationCode);
                invoice.CompanyId = reservation.CompanyId;
                invoice.CompanyName = reservation.CompanyName;
            }

            return;
        }

        if (NormalizeOptionalGuid(invoice.ContactId) is { } contactId)
        {
            if (NormalizeOptionalString(invoice.ContactName) == null)
            {
                var contact = await _contactRepository.GetContactByIdAsync(contactId, invoice.OrganizationId);
                invoice.ContactName = NormalizeOptionalString(contact?.DisplayName ?? contact?.CompanyName ?? contact?.FullName);
            }

            invoice.ResponsibleParty = NormalizeOptionalString(invoice.ContactName);
            return;
        }

        if (NormalizeOptionalString(invoice.ContactName) == null && NormalizeOptionalString(invoice.ResponsibleParty) != null)
            invoice.ContactName = NormalizeOptionalString(invoice.ResponsibleParty);

        invoice.ResponsibleParty = NormalizeOptionalString(invoice.ContactName) ?? NormalizeOptionalString(invoice.ResponsibleParty);

        if (NormalizeOptionalGuid(invoice.PropertyId) is { } propertyId && NormalizeOptionalString(invoice.PropertyCode) == null)
        {
            var property = await _propertyRepository.GetPropertyByIdAsync(propertyId, invoice.OrganizationId);
            invoice.PropertyCode = NormalizeOptionalString(property?.PropertyCode);
        }
    }
}
