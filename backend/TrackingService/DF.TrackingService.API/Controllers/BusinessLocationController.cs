using Microsoft.AspNetCore.Mvc;
using DF.TrackingService.Application.Services.Interfaces;
using DF.TrackingService.Contracts.Models.Requests;

namespace DF.TrackingService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessLocationController(IBusinessLocationService businessLocationService) : ControllerBase
{
    // GET api/businesslocation/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBusinessLocation(Guid id)
    {
        var businessLocation = await businessLocationService.GetBusinessLocationAsync(id);
        if (businessLocation is null)
            return NotFound();

        return Ok(businessLocation);
    }

    // GET api/businesslocation/business/{businessId}
    [HttpGet("business/{businessId:guid}")]
    public async Task<IActionResult> GetBusinessLocationsByBusinessId(Guid businessId)
    {
        var businessLocations = await businessLocationService.GetBusinessLocationsByBusinessIdAsync(businessId);
        return Ok(businessLocations);
    }

    // POST api/businesslocation
    [HttpPost]
    public async Task<IActionResult> CreateBusinessLocation([FromBody] CreateBusinessLocationRequest request)
    {
        var created = await businessLocationService.CreateBusinessLocationAsync(request);
        return CreatedAtAction(nameof(GetBusinessLocation), new { id = created.Id }, created);
    }

    // DELETE api/businesslocation/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteBusinessLocation(Guid id)
    {
        var success = await businessLocationService.DeleteBusinessLocationAsync(id);
        if (!success)
            return NotFound();

        return NoContent();
    }
}
