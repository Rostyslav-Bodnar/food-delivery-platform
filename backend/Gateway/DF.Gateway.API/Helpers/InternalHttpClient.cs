using DF.Gateway.API.Constants;

namespace DF.Gateway.API.Helpers;

public class InternalHttpClient(
    HttpClient client,
    InternalAuthSigner signer,
    IConfiguration config)
{
    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        string userId)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = signer.Sign(userId, timestamp);

        // 👇 ОЦЕ ГОЛОВНЕ (у тебе цього не було в gateway usage)
        request.Headers.Add("X-Internal-Key", config["Internal:ApiKey"]);

        request.Headers.Add(InternalAuthConstants.UserIdHeader, userId);
        request.Headers.Add(InternalAuthConstants.TimestampHeader, timestamp);
        request.Headers.Add(InternalAuthConstants.SignatureHeader, signature);

        return await client.SendAsync(request);
    }
}