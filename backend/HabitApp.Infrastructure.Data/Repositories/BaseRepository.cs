using HabitApp.Domain.Entities;
using HabitApp.Infrastructure.Data.Context;
using HabitApp.Infrastructure.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HabitApp.Infrastructure.Data.Repositories;

public abstract class BaseRepository<TEntity>(SqliteContext context) 
    : IBaseRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly SqliteContext _context = context;

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        => await _context.Set<TEntity>().ToListAsync();

    public virtual async Task<TEntity?> GetByIdAsync(int id)
        => await _context.Set<TEntity>().FindAsync(id);

    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        await _context.Set<TEntity>().AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        var existingEntity = await _context.Set<TEntity>().FindAsync(entity.Id)
            ?? throw new KeyNotFoundException($"Entidade com ID {entity.Id} não encontrada.");

        _context.Entry(existingEntity).CurrentValues.SetValues(entity);
        await _context.SaveChangesAsync();

        return existingEntity;
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        var entity = await _context.Set<TEntity>()
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null) return false;

        if (entity.IsDeleted) return true;

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();

        return true;
    }
}
