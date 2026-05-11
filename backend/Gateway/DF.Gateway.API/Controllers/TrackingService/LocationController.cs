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
public class LocationController(GatewayProxy proxy) : ControllerBase
{
    // =========================
    // GET LOCATION BY ID
    // =========================
    [HttpGet("{id:guid}")]
    public Task<Response<LocationResponse>> GetLocation(Guid id)
        => proxy.ProxyAsync<LocationResponse>(HttpContext);

    // =========================
    // GET ALL LOCATIONS
    // =========================
    [HttpGet]
    public Task<Response<IEnumerable<LocationResponse>>> GetLocations()
        => proxy.ProxyAsync<IEnumerable<LocationResponse>>(HttpContext);

    // =========================
    // CREATE LOCATION
    // =========================
    [HttpPost]
    public Task<Response<LocationResponse>> CreateLocation(
        [FromBody] CreateLocationRequest request)
        => proxy.ProxyAsync<LocationResponse>(HttpContext);

    // =========================
    // UPDATE LOCATION
    // =========================
    [HttpPut]
    public Task<Response<LocationResponse>> UpdateLocation(
        [FromBody] UpdateLocationRequest request)
        => proxy.ProxyAsync<LocationResponse>(HttpContext);

    // =========================
    // DELETE LOCATION
    // =========================
    [HttpDelete("{id:guid}")]
    public Task<Response<bool>> DeleteLocation(Guid id)
        => proxy.ProxyAsync<bool>(HttpContext);

    // =========================
    // ADD BUSINESS LOCATION
    // =========================
    [HttpPost("add")]
    public Task<Response<LocationResponse>> AddBusinessLocation(
        [FromBody] AddLocationRequest request)
        => proxy.ProxyAsync<LocationResponse>(HttpContext);
}