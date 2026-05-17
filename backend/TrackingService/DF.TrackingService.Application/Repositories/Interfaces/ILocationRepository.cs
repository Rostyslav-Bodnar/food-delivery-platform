using DF.TrackingService.Domain.Entities;

namespace DF.TrackingService.Application.Repositories.Interfaces;

public interface ILocationRepository : IRepository<Location>
{
    Task<Location?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Location>> ListAsync(int skip, int take, CancellationToken cancellationToken = default);
}
