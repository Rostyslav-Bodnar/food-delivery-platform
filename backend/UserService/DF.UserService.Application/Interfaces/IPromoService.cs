namespace DF.UserService.Application.Interfaces;

public interface IPromoService
{
    Task AssignWelcomePromoAsync(Guid accountId, string email);
}