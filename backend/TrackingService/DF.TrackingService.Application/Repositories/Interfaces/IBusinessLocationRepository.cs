using DF.TrackingService.Domain.Entities;

namespace DF.TrackingService.Application.Repositories.Interfaces;

public interface IBusinessLocationRepository : IRepository<BusinessLocation>
{
    Task<BusinessLocation?> GetByLocationIdAsync(Guid locationId);
    Task<IEnumerable<BusinessLocation>> GetByBusinessIdAsync(Guid businessId);

}