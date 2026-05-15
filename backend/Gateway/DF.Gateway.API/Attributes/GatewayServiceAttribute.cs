using DF.Gateway.API.Helpers;

namespace DF.Gateway.API.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class GatewayServiceAttribute(ServiceType serviceType) : Attribute
{
    public ServiceType ServiceType { get; } = serviceType;
}
