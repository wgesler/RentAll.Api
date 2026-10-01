using RentAll.Domain.Enums;

namespace RentAll.Api.Dtos.Reservations.Reservations;

internal static class ReservationReferralValidation
{
    public static bool IsReferralFeeAllowedForProperty(PropertyLeaseType propertyLeaseType)
        => propertyLeaseType is PropertyLeaseType.Direct or PropertyLeaseType.ThirdParty;

    public static (bool IsValid, string? ErrorMessage) ValidateReferralFeeForPropertyLease(bool referralFee, PropertyLeaseType? propertyLeaseType)
    {
        if (!referralFee || propertyLeaseType is null)
            return (true, null);

        if (!IsReferralFeeAllowedForProperty(propertyLeaseType.Value))
            return (false, "Referral fee is only available for Direct or Third Party properties");

        return (true, null);
    }
}
