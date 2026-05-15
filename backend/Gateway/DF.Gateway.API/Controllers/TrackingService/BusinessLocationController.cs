using DF.Contracts.Gateway.Requests.Tracking;
using DF.Contracts.Gateway.Responses;
using DF.Contracts.Gateway.Responses.Tracking;
using DF.Gateway.API.Attributes;
using DF.Gateway.API.Helpers;
using DF.Gateway.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DF.Gateway.API.Controllers.TrackingService;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[GatewayService(ServiceType.TrackingService)]
public class BusinessLocationController(GatewayProxy proxy) : ControllerBase
{
    // =========================
    // GET BUSINESS LOCATION BY ID
    // =========================
    [HttpGet("{id:guid}")]
    public Task<Response<BusinessLocationResponse>> GetBusinessLocation(Guid id)
        => proxy.ProxyAsync<BusinessLocationResponse>(HttpContext);

    // =========================
    // GET BUSINESS LOCATIONS
    // =========================
    [HttpGet("business/{businessId:guid}")]
    public Task<Response<IEnumerable<BusinessLocationResponse>>> GetBusinessLocationsByBusinessId(
        Guid businessId)
        => proxy.ProxyAsync<IEnumerable<BusinessLocationResponse>>(HttpContext);

    // =========================
    // CREATE BUSINESS LOCATION
    // =========================
    [HttpPost]
    public Task<Response<BusinessLocationResponse>> CreateBusinessLocation(
        [FromBody] CreateBusinessLocationRequest request)
        => proxy.ProxyAsync<BusinessLocationResponse>(HttpContext);

    // =========================
    // DELETE BUSINESS LOCATION
    // =========================
    [HttpDelete("{id:guid}")]
    public Task<Response<bool>> DeleteBusinessLocation(Guid id)
        => proxy.ProxyAsync<bool>(HttpContext);
}