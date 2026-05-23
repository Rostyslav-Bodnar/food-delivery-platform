using System.Security.Cryptography;
using System.Text;

namespace DF.UserService.API.Helpers;

public class InternalAuthSigner(IConfiguration config)
{
    private readonly string _secret =
        config["Internal:ApiKey"]!;

    public string Sign(string timestamp, string nonce)
    {
        var payload = $"{timestamp}:{nonce}";

        using var hmac =
            new HMACSHA256(
                Encoding.UTF8.GetBytes(_secret)
            );

        var hash = hmac.ComputeHash(
            Encoding.UTF8.GetBytes(payload)
        );

        return Convert.ToBase64String(hash);
    }

    public bool Validate(
        string timestamp,
        string nonce,
        string signature
    )
    {
        var expectedBytes = Encoding.UTF8.GetBytes(Sign(timestamp, nonce));
        var actualBytes = Encoding.UTF8.GetBytes(signature ?? string.Empty);

        // FixedTimeEquals throws on length mismatch; treating a malformed
        // signature as "invalid" is a 401, not a 500.
        if (expectedBytes.Length != actualBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}