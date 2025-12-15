using DF.OrderService.Application.Repositories.Interfaces;
using DF.OrderService.Domain.Entities;
using DF.OrderService.Infrastructure.Data;

namespace DF.OrderService.Application.Repositories;

public class LocationRepository(AppDbContext dbContext) : ILocationRepository
{
    public async Task<Location?> Get(Guid id)
    {
        return await dbContext.Locations.FindAsync(id);
    }

    public async Task<IEnumerable<Location?>> GetAll()
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }
}