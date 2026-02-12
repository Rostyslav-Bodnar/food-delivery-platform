using DF.TrackingService.Contracts.Models.Requests;
using DF.TrackingService.Contracts.Models.Responses;

namespace DF.TrackingService.Application.Services.Interfaces;

public interface IBusinessLocationService
{
    Task<BusinessLocationResponse?> GetBusinessLocationAsync(Guid id);
    Task<List<BusinessLocationResponse>> GetBusinessLocationsByBusinessIdAsync(Guid businessId);
    Task<BusinessLocationResponse> CreateBusinessLocationAsync(CreateBusinessLocationRequest request);
    Task<bool> DeleteBusinessLocationAsync(Guid id);
}