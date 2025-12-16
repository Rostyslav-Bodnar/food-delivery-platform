using DF.TrackingService.Application.Repositories.Interfaces;
using DF.TrackingService.Domain.Entities;
using DF.TrackingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DF.TrackingService.Application.Repositories;

public class CourierLocationRepository(MongoDbContext dbContext) : ICourierLocationRepository
{
    public async Task<CourierLocation?> Get(Guid id)
    {
        return await dbContext.TrackingEvents.FindAsync(id);
    }

    public async Task<IEnumerable<CourierLocation?>> GetAll()
    {
        return await dbContext.TrackingEvents.ToListAsync();
    }

    public async Task<CourierLocation> Create(CourierLocation entity)
    {
        var created = await dbContext.TrackingEvents.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return created.Entity;
    }

    public async Task<CourierLocation> Update(CourierLocation entity)
    {
        dbContext.TrackingEvents.Update(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> Delete(Guid id)
    {
        var existing = await dbContext.TrackingEvents.FirstOrDefaultAsync(c => c.Id == id);
        if (existing is null) return false;

        dbContext.TrackingEvents.Remove(existing);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<CourierLocation?> GetByCourierId(Guid courierId)
    {
        return await dbContext.TrackingEvents.FirstOrDefaultAsync(c => c.CourierId == courierId);
    }

    public async Task<CourierLocation?> UpdateByCourierId(Guid courierId, double latitude, double longitude)
    {
        var existing = await dbContext.TrackingEvents.FirstOrDefaultAsync(c => c.CourierId == courierId) ?? 
                       await Create(new CourierLocation
                        {
                            CourierId = courierId,
                            Latitude = latitude,
                            Longitude = longitude
                        });

        existing.Latitude = latitude;
        existing.Longitude = longitude;

        dbContext.TrackingEvents.Update(existing);
        await dbContext.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteByCourierId(Guid courierId)
    {
        var existing = await dbContext.TrackingEvents.FirstOrDefaultAsync(c => c.CourierId == courierId);
        if (existing is null) return false;

        dbContext.TrackingEvents.Remove(existing);
        await dbContext.SaveChangesAsync();
        return true;
    }
}
