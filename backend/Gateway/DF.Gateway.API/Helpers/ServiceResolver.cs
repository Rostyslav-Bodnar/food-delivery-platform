using DF.Gateway.API.Attributes;

namespace DF.Gateway.API.Helpers;

public class ServiceResolver(IConfiguration config)
{
    public Uri Resolve(HttpContext context)
    {
        var endpoint = context.GetEndpoint()
                       ?? throw new InvalidOperationException("No endpoint");

        var attr = endpoint.Metadata.GetMetadata<GatewayServiceAttribute>()
                   ?? throw new InvalidOperationException("GatewayServiceAttribute missing");

        var baseUrl = config[$"Services:{attr.ServiceType}"]
                      ?? throw new InvalidOperationException($"Service {attr.ServiceType} not configured");

        var pathAndQuery =
            $"{context.Request.Path}{context.Request.QueryString}";

        return new Uri(new Uri(baseUrl), pathAndQuery);
    }
}