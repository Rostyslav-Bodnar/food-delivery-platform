using System.Security.Cryptography;
using System.Text;

namespace DF.UserService.API.Helpers;

public class InternalAuthSigner(string secret)
{
    public string Sign(string userId, string timestamp)
    {
        var payload = $"{userId}:{timestamp}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));

        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    public bool Validate(string userId, string timestamp, string signature)
    {
        var expected = Sign(userId, timestamp);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature)
        );
    }
}