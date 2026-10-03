using Core.Entities;
using Core.Interfaces;

namespace Infrastructure.Data;

public class UnitOfWork(StoreContext context) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = [];

    public IGenericRepository<T> Repository<T>() where T : BaseEntity
    {
        var type = typeof(T);
        if (!_repositories.TryGetValue(type, out var repository))
        {
            repository = new GenericRepository<T>(context);
            _repositories[type] = repository;
        }
        return (IGenericRepository<T>)repository;
    }

    public async Task<int> CompleteAsync() => await context.SaveChangesAsync();

    public void Dispose() => context.Dispose();
}
