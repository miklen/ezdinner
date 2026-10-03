using NodaTime;
using NodaTime.Text;
using System.Security.Cryptography;
using System.Text;

namespace EzDinner.Core.Aggregates.DinnerAggregate;

public static class DinnerIdentityFactory
{
    public static Guid Create(Guid familyId, LocalDate date)
    {
        var identity = $"EzDinner.Dinner/v1/{familyId:D}/{LocalDatePattern.Iso.Format(date)}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        hash[7] = (byte)((hash[7] & 0x0f) | 0x80);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);
        return new Guid(hash.AsSpan(0, 16));
    }
}
