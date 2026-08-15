using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class SqlProductSearchService(StoreContext context) : IProductSearchService
{
    public async Task<IReadOnlyList<Product>> SearchAsync(ProductSpecParams specParams)
    {
        var spec = new ProductSpecification(specParams);
        var query = context.Products.AsNoTracking().AsQueryable();
        query = SpecificationEvaluator<Product>.GetQuery(query, spec);
        return await query.ToListAsync();
    }

    public async Task<int> CountAsync(ProductSpecParams specParams)
    {
        var spec = new ProductSpecification(specParams);
        var query = context.Products.AsNoTracking().AsQueryable();
        query = spec.ApplyCritera(query);
        return await query.CountAsync();
    }
}
