using DF.UserService.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace DF.UserService.Tests.Helpers;

internal static class MockUserManager
{
    public static Mock<UserManager<User>> Create()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
