using DF.MenuService.Application.Services.Interfaces;
using DF.MenuService.Domain.Entities;
using DF.MenuService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DF.MenuService.Application.Repositories.Interfaces;

public class MenuRepository(AppDbContext dbContext) : IMenuRepository
{
    public async Task<Menu?> Get(Guid id)
    {
        return await dbContext.Menus.FindAsync(id);
    }

    public async Task<IEnumerable<Menu?>> GetAll()
    {
        return await dbContext.Menus.ToListAsync();
    }

    public async Task<Menu> Create(Menu entity)
    {
        await dbContext.Menus.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<Menu> Update(Menu entity)
    {
        dbContext.Menus.Update(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> Delete(Guid id)
    {
        var menu = await dbContext.Menus.FindAsync(id);
        if (menu == null)
            return false;

        dbContext.Menus.Remove(menu);
        await dbContext.SaveChangesAsync();
        return true;
    }
}