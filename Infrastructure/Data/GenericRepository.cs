using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class GenericRepository<T>(StoreContext context) : IGenericRepository<T> where T : BaseEntity
{
    public void Add(T entity) => context.Set<T>().Add(entity);

    public async Task<int> CountAsync(ISpecification<T> spec)
    {
        var query = context.Set<T>().AsNoTracking().AsQueryable();
        query = spec.ApplyCritera(query);
        return await query.CountAsync();
    }

    public bool Exits(int id) => context.Set<T>().Any(x => x.Id == id);

    public async Task<T?> GetByIdAsync(int id) => await context.Set<T>().FindAsync(id);

    public async Task<T?> GetEntityWithSpec(ISpecification<T> spec) =>
        await ApplySpecification(spec, trackChanges: true).FirstOrDefaultAsync();

    public async Task<TResult?> GetEntityWithSpec<TResult>(ISpecification<T, TResult> spec) =>
        await ApplySpecification(spec).FirstOrDefaultAsync();

    public async Task<IReadOnlyList<T>> ListAllAsync() =>
        await context.Set<T>().AsNoTracking().ToListAsync();

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec) =>
        await ApplySpecification(spec).ToListAsync();

    public async Task<IReadOnlyList<TResult>> ListAsync<TResult>(ISpecification<T, TResult> spec) =>
        await ApplySpecification(spec).ToListAsync();

    public void Remove(T entity) => context.Set<T>().Remove(entity);

    public async Task<bool> SaveAllAsync() => await context.SaveChangesAsync() > 0;

    public void Update(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        context.Set<T>().Attach(entity);
        context.Entry(entity).State = EntityState.Modified;
    }

    private IQueryable<T> ApplySpecification(ISpecification<T> spec, bool trackChanges = false)
    {
        var query = trackChanges ? context.Set<T>().AsQueryable() : context.Set<T>().AsNoTracking();
        return SpecificationEvaluator<T>.GetQuery(query, spec);
    }

    private IQueryable<TResult> ApplySpecification<TResult>(ISpecification<T, TResult> spec)
    {
        var query = context.Set<T>().AsNoTracking().AsQueryable();
        return SpecificationEvaluator<T>.GetQuery<T, TResult>(query, spec);
    }
}
