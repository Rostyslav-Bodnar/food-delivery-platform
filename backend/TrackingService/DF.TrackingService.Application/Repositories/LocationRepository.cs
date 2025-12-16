using DF.TrackingService.Application.Repositories.Interfaces;
using DF.TrackingService.Domain.Entities;
using DF.TrackingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DF.TrackingService.Application.Repositories;

public class LocationRepository(SqlDbContext dbContext) : ILocationRepository
{
    public async Task<Location?> Get(Guid id)
    {
        return await dbContext.Locations.FindAsync(id);
    }

    public async Task<IEnumerable<Location?>> GetAll()
    {
        return await dbContext.Locations.ToListAsync();
    }

    public async Task<Location> Create(Location entity)
    {
        var location = await dbContext.Locations.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return location.Entity;
    }

    public async Task<Location> Update(Location entity)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> Delete(Guid id)
    {
        var location = await dbContext.Locations.FirstOrDefaultAsync(l => l.Id == id);
        
        if(location == null)
            return false;
        
        var result = dbContext.Locations.Remove(location);
        await dbContext.SaveChangesAsync();
        return true;
    }
}