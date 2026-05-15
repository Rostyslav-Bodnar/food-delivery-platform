using DF.Gateway.API.Helpers;

namespace DF.Gateway.API.Infrastructure;

public class InternalGatewayContext(
    IHttpContextAccessor accessor,
    InternalAuthSigner signer,
    IConfiguration config)
{
    public InternalRequestHeaders Build()
    {
        var http = accessor.HttpContext
                   ?? throw new InvalidOperationException();

        var timestamp =
            DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var nonce =
            Guid.NewGuid().ToString("N");

        var signature =
            signer.Sign(timestamp, nonce);

        return new InternalRequestHeaders(
            timestamp,
            nonce,
            signature,
            config["Internal:ApiKey"]!
        );
    }
}

public record InternalRequestHeaders(
    string Timestamp,
    string Nonce,
    string Signature,
    string ApiKey
);