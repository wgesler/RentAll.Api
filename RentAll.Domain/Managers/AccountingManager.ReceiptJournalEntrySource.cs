using System.Security.Cryptography;
using System.Text;
using RentAll.Domain.Models;

namespace RentAll.Domain.Managers;

public partial class AccountingManager
{
    static readonly Guid BillPaymentSourceNamespace = Guid.Parse("7f3e2a1b-9c4d-4e5f-8a6b-1d2e3f4a5b6c");

    static Guid CreateBillPaymentSourceId(Guid receiptGuid, int paymentSequence)
    {
        if (receiptGuid == Guid.Empty)
            throw new Exception("ReceiptGuid is required to create a bill payment source id");

        if (paymentSequence < 0)
            throw new Exception("PaymentSequence is required to create a bill payment source id");

        var input = $"{BillPaymentSourceNamespace:N}:{receiptGuid:N}:{paymentSequence}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
        return new Guid(guidBytes);
    }

    static int TryGetBillPaymentSequence(Guid sourceId, Guid receiptGuid)
    {
        for (var sequence = 0; sequence <= 999; sequence++)
        {
            if (CreateBillPaymentSourceId(receiptGuid, sequence) == sourceId)
                return sequence;
        }

        return -1;
    }
}
