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
        // Caller loads via Get(...) and mutates; the change tracker already
        // has the deltas. Attach if detached so partial updates don't clobber
        // unrelated columns.
        if (dbContext.Entry(entity).State == EntityState.Detached)
        {
            dbContext.Locations.Attach(entity);
        }
        await dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> Delete(Guid id)
    {
        var location = await dbContext.Locations.FirstOrDefaultAsync(l => l.Id == id);

        if (location == null)
            return false;

        dbContext.Locations.Remove(location);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public Task<Location?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return dbContext.Locations
            .FirstOrDefaultAsync(l => l.OrderId == orderId, cancellationToken);
    }

    public async Task<IReadOnlyList<Location>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await dbContext.Locations
            .OrderBy(l => l.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
