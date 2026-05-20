using DF.Contracts.Gateway.Requests.Tracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DF.TrackingService.Application.Services.Interfaces;

namespace DF.TrackingService.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LocationController(ILocationService locationService) : ControllerBase
{
    // GET api/location/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetLocation(Guid id)
    {
        var location = await locationService.GetLocationAsync(id);
        if (location is null)
            return NotFound();

        return Ok(location);
    }

    // GET api/location?skip=0&take=100
    [HttpGet]
    public async Task<IActionResult> GetLocations([FromQuery] int skip = 0, [FromQuery] int take = 100)
    {
        if (skip < 0) skip = 0;
        if (take is <= 0 or > 500) take = 100;

        var locations = await locationService.GetLocationsAsync(skip, take);
        return Ok(locations);
    }

    // POST api/location
    [HttpPost]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationRequest request)
    {
        var created = await locationService.CreateLocation(request);
        return CreatedAtAction(nameof(GetLocation), new { id = created.Id }, created);
    }

    // PUT api/location/{id}
    // Id travels in the route — the body request type doesn't carry one
    // (a follow-up could add Id to the shared DF.Contracts type).
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] UpdateLocationRequest request)
    {
        var updated = await locationService.UpdateLocation(id, request);
        if (updated is null)
            return NotFound();

        return Ok(updated);
    }

    // DELETE api/location/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteLocation(Guid id)
    {
        var success = await locationService.DeleteLocation(id);
        if (!success)
            return NotFound();

        return NoContent();
    }
    
    [HttpPost("add")]
    public async Task<IActionResult> AddBusinessLocation([FromBody] AddLocationRequest request)
    {
        var result = await locationService.AddLocationAsync(request);
        return Ok(result);
    }
}