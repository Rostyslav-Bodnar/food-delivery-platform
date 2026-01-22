using DF.TrackingService.Domain.Entities;

namespace DF.TrackingService.Application.Repositories.Interfaces;

public interface ICourierLocationRepository : IRepository<CourierLocation>
{
    Task<CourierLocation?> GetByCourierId(Guid courierId);
    Task<CourierLocation?> UpdateByCourierId(Guid courierId, double latitude, double longitude);
    Task<bool> DeleteByCourierId(Guid courierId);

}