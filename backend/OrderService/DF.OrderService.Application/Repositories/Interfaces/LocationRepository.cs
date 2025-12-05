using DF.OrderService.Domain.Entities;

namespace DF.OrderService.Application.Repositories.Interfaces;

public class LocationRepository : ILocationRepository
{
    public async Task<Location?> Get(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Location?>> GetAll()
    {
        throw new NotImplementedException();
    }

    public async Task<Location> Create(Location entity)
    {
        throw new NotImplementedException();
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