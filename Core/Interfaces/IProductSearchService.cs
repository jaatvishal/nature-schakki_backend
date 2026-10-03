using Core.Entities;
using Core.Specifications;

namespace Core.Interfaces;

public interface IProductSearchService
{
    Task<IReadOnlyList<Product>> SearchAsync(ProductSpecParams specParams);
    Task<int> CountAsync(ProductSpecParams specParams);
}
