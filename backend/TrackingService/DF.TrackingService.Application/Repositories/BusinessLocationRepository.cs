using DF.TrackingService.Application.Repositories.Interfaces;
using DF.TrackingService.Domain.Entities;
using DF.TrackingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DF.TrackingService.Application.Repositories;

public class BusinessLocationRepository(SqlDbContext dbContext) : IBusinessLocationRepository
{
    public async Task<BusinessLocation?> Get(Guid id)
    {
        return await dbContext.BusinessLocations.FindAsync(id);
    }

    public async Task<IEnumerable<BusinessLocation?>> GetAll()
    {
        return await dbContext.BusinessLocations.ToListAsync();
    }

    public async Task<BusinessLocation> Create(BusinessLocation entity)
    {
        var result = await dbContext.BusinessLocations.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return result.Entity;
    }

    public async Task<BusinessLocation> Update(BusinessLocation entity)
    {
        var result = dbContext.Update(entity);
        await dbContext.SaveChangesAsync();
        return result.Entity;
    }

    public async Task<bool> Delete(Guid id)
    {
        var entity = await dbContext.BusinessLocations.FindAsync(id);
        if (entity == null)
            return false;
        
        dbContext.BusinessLocations.Remove(entity);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<BusinessLocation?> GetByLocationIdAsync(Guid locationId)
    {
        var result = await dbContext.BusinessLocations.FirstOrDefaultAsync(
            bl => bl.LocationId == locationId);
        
        if(result == null)
            return null;
        
        return result;
    }

    public async Task<IEnumerable<BusinessLocation>> GetByBusinessIdAsync(Guid businessId)
    {
        var result = await dbContext.BusinessLocations.Where(
            bl => bl.BusinessId == businessId).ToListAsync();
        return result;
    }
}