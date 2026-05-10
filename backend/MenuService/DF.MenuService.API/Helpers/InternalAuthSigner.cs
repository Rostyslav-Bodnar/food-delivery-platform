using System.Security.Cryptography;
using System.Text;

namespace DF.MenuService.API.Helpers;

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
        var expected = Sign(timestamp, nonce);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature)
        );
    }
}