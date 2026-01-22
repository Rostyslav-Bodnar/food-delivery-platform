using Microsoft.AspNetCore.Mvc;
using DF.TrackingService.Application.Services.Interfaces;
using DF.TrackingService.Contracts.Models.Requests;

namespace DF.TrackingService.API.Controllers;

[ApiController]
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

    // GET api/location
    [HttpGet]
    public async Task<IActionResult> GetLocations()
    {
        var locations = await locationService.GetLocationsAsync();
        return Ok(locations);
    }

    // POST api/location
    [HttpPost]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationRequest request)
    {
        var created = await locationService.CreateLocation(request);
        return CreatedAtAction(nameof(GetLocation), new { id = created.Id }, created);
    }

    // PUT api/location
    [HttpPut]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
    {
        var updated = await locationService.UpdateLocation(request);
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
}